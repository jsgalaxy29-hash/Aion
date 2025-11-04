using Aion.Domain.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Aion.Infrastructure.Services
{
    /// <summary>
    /// Implémentation minimale de IAuthService. Utilise Cookie Authentication pour créer l'identité d'un utilisateur.
    /// Cette version valide un login unique "admin" / "admin" et retourne un tenant fixe.
    /// </summary>
    public sealed class AuthService : IAuthService
    {
        private readonly IHttpContextAccessor _http;
        private readonly IDbContextFactory<AionDbContext> _dbFactory;

        public AuthService(IHttpContextAccessor http, IDbContextFactory<AionDbContext> dbFactory)
        {
            _http = http;
            _dbFactory = dbFactory;
        }

        public async Task<SignInResult> SignInAsync(string login, string password, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                return SignInResult.Fail("Identifiant ou mot de passe manquant");
            }

            // Retrieve the user for the default tenant (Guid.Empty) with the specified login.
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var user = await db.SUser.FirstOrDefaultAsync(u => u.TenantId == 0 && u.Login == login, ct);
            if (user == null)
            {
                return SignInResult.Fail("Invalid credentials");
            }

            // Compute the SHA‑256 hash of the provided password in hexadecimal form and compare.
            string hash;
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                var hashed = sha.ComputeHash(bytes);
                hash = BitConverter.ToString(hashed).Replace("-", string.Empty).ToLowerInvariant();
            }
            if (!string.Equals(hash, user.PasswordHash, StringComparison.OrdinalIgnoreCase))
            {
                return SignInResult.Fail("Invalid credentials");
            }

            // Build identity claims.  Use Name claim from the user entity and store user and tenant identifiers.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name?.Name ?? string.Empty),
                new Claim("sub", user.Id.ToString()),
                new Claim("tenant", user.TenantId.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await _http.HttpContext!.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return SignInResult.Success;
        }

        public async Task SignOutAsync(CancellationToken ct)
        {
            await _http.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
