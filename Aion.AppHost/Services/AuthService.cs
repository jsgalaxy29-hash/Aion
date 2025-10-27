using Aion.DataEngine.Entities;
using Aion.Domain.Contracts;
using Aion.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            SecurityDbContext db,
            IHttpContextAccessor httpContext,
            ILogger<AuthService> logger)
        {
            _db = db;
            _httpContext = httpContext;
            _logger = logger;
        }

        public async Task<bool> LoginAsync(string username, string password, int tenantId, bool rememberMe = false)
        {
            try
            {
                _logger.LogInformation("Tentative de connexion pour {Username} (Tenant: {TenantId})", username, tenantId);

                // Recherche de l'utilisateur
                var user = await _db.SUser
                    .FirstOrDefaultAsync(u =>
                        u.NormalizedUserName == username.ToUpperInvariant() &&
                        u.TenantId == tenantId &&
                        u.IsActive &&
                        !u.Deleted);

                if (user == null)
                {
                    _logger.LogWarning("Utilisateur {Username} introuvable ou inactif", username);
                    return false;
                }

                // Vérification du mot de passe
                if (!VerifyPassword(password, user.PasswordHash))
                {
                    _logger.LogWarning("Mot de passe incorrect pour {Username}", username);

                    user.AccessFailedCount++;
                    if (user.AccessFailedCount >= 5)
                    {
                        user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
                        _logger.LogWarning("Compte {Username} verrouillé après 5 échecs", username);
                    }

                    await _db.SaveChangesAsync();
                    return false;
                }

                // Vérification du lockout
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    _logger.LogWarning("Compte {Username} verrouillé jusqu'à {LockoutEnd}", username, user.LockoutEnd);
                    return false;
                }

                // Reset des échecs et mise à jour dernière connexion
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
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                    new Claim("fullname", user.FullName ?? user.UserName)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = rememberMe,
                    ExpiresUtc = rememberMe
                        ? DateTimeOffset.UtcNow.AddDays(30)
                        : DateTimeOffset.UtcNow.AddHours(8),
                    AllowRefresh = true
                };

                var httpContext = _httpContext.HttpContext;
                if (httpContext == null)
                {
                    _logger.LogError("HttpContext est null lors du login");
                    return false;
                }

                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    authProperties);

                _logger.LogInformation("✅ Connexion réussie pour {Username} (ID: {UserId})", username, user.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la connexion de {Username}", username);
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                var httpContext = _httpContext.HttpContext;
                if (httpContext != null)
                {
                    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    _logger.LogInformation("Déconnexion réussie");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la déconnexion");
            }
        }

        public async Task<SUser?> GetCurrentUserAsync()
        {
            try
            {
                var httpContext = _httpContext.HttpContext;
                if (httpContext == null) return null;

                var userIdClaim = httpContext.User.FindFirst("sub");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                    return null;

                return await _db.SUser
                    .FirstOrDefaultAsync(u => u.Id == userId && !u.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'utilisateur courant");
                return null;
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            // TEMPORAIRE : comparaison simple pour le développement
            // EN PRODUCTION : utiliser BCrypt.Net-Next
            // return BCrypt.Net.BCrypt.Verify(password, hash);

            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                return false;

            return password == hash;
        }
    }
}