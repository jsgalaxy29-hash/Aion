using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace Aion.Infrastructure.Services
{
    /// <summary>
    /// Implémentation minimale de IAuthService. Utilise Cookie Authentication pour créer l'identité d'un utilisateur.
    /// Cette version valide un login unique "admin" / "admin" et retourne un tenant fixe.
    /// </summary>
    public sealed class AuthService : IAuthService
    {
        private readonly IHttpContextAccessor _http;

        public AuthService(IHttpContextAccessor http)
        {
            _http = http;
        }

        public async Task<SignInResult> SignInAsync(string login, string password, CancellationToken ct)
        {
            // Exemple de validation très simplifiée
            if (login.Equals("admin", StringComparison.OrdinalIgnoreCase) && password == "admin")
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "Admin"),
                    new Claim("sub", Guid.NewGuid().ToString()),
                    new Claim("tenant", Guid.Empty.ToString())
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await _http.HttpContext!.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                return SignInResult.Success;
            }
            return SignInResult.Fail("Invalid credentials");
        }

        public async Task SignOutAsync(CancellationToken ct)
        {
            await _http.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
