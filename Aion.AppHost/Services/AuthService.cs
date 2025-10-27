using Aion.DataEngine.Entities;
using Aion.Domain.Contracts;
using Aion.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Aion.AppHost.Services
{

    /// <summary>
    /// Service d'authentification Aion.
    /// Gère le login/logout et la création des claims.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly SecurityDbContext _db;
        private readonly IHttpContextAccessor _httpContext;

        public AuthService(SecurityDbContext db, IHttpContextAccessor httpContext)
        {
            _db = db;
            _httpContext = httpContext;
        }

        public async Task<bool> LoginAsync(string username, string password, int tenantId, bool rememberMe = false)
        {
            var user = await _db.SUser
                .FirstOrDefaultAsync(u =>
                    u.NormalizedUserName == username.ToUpperInvariant() &&
                    u.TenantId == tenantId &&
                    u.IsActive &&
                    !u.Deleted);

            if (user == null)
                return false;

            // Vérification du mot de passe (à remplacer par BCrypt en production)
            if (!VerifyPassword(password, user.PasswordHash))
            {
                user.AccessFailedCount++;
                if (user.AccessFailedCount >= 5)
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);

                await _db.SaveChangesAsync();
                return false;
            }

            // Vérification du lockout
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                return false;

            // Reset des échecs
            user.AccessFailedCount = 0;
            user.LockoutEnd = null;
            user.LastLoginDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Création des claims de base
            var claims = new[]
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("tenant", user.TenantId.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("fullname", user.FullName ?? user.UserName)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8),
                AllowRefresh = true
            };

            await _httpContext.HttpContext!.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);

            return true;
        }

        public async Task LogoutAsync()
        {
            await _httpContext.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public async Task<SUser?> GetCurrentUserAsync()
        {
            var userIdClaim = _httpContext.HttpContext?.User.FindFirst("sub");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return null;

            return await _db.SUser.FirstOrDefaultAsync(u => u.Id == userId && !u.Deleted);
        }

        private bool VerifyPassword(string password, string hash)
        {
            // TEMPORAIRE : comparaison simple
            // EN PRODUCTION : utiliser BCrypt.Net-Next
            // return BCrypt.Net.BCrypt.Verify(password, hash);
            return password == hash; // À REMPLACER !
        }

        public Task<SignInResult> SignInAsync(string login, string password, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task SignOutAsync(CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> LoginAsync(string login, string password, int tenantId, bool rememberMe, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task LogoutAsync(CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}