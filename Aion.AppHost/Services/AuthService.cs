using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Aion.Security;
using Aion.DataEngine.Entities;
using Aion.Domain.Contracts;

namespace Aion.AppHost.Services
{
    //public interface IAuthService
    //{
    //    Task<bool> LoginAsync(string username, string password, int tenantId, bool rememberMe = false);
    //    Task LogoutAsync();
    //    Task<SUser?> GetCurrentUserAsync();
    //}

    /// <summary>
    /// Service d'authentification Aion.
    /// Version corrigée pour éviter l'erreur "Headers are read-only".
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

                // IMPORTANT : Vérifier HttpContext AVANT toute opération
                var httpContext = _httpContext.HttpContext;
                if (httpContext == null)
                {
                    _logger.LogError("HttpContext est null");
                    return false;
                }

                // Vérifier que la réponse n'a pas déjà commencé
                if (httpContext.Response.HasStarted)
                {
                    _logger.LogError("La réponse HTTP a déjà commencé, impossible de se connecter");
                    return false;
                }

                // Recherche de l'utilisateur (sans tracking pour éviter les conflits)
                var user = await _db.SUser
                    .AsNoTracking()
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

                    // Mise à jour du compteur d'échecs (détaché)
                    await IncrementFailedLoginAsync(user.Id);

                    return false;
                }

                // Vérification du lockout
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    _logger.LogWarning("Compte {Username} verrouillé jusqu'à {LockoutEnd}", username, user.LockoutEnd);
                    return false;
                }

                // Création des claims AVANT toute mise à jour de DB
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
                    AllowRefresh = true,
                    IssuedUtc = DateTimeOffset.UtcNow
                };

                // CRITIQUE : SignInAsync DOIT être appelé AVANT toute autre opération
                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    authProperties);

                try
                {
                    await UpdateLastLoginAsync(user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur mise à jour dernière connexion");
                }

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
                if (httpContext != null && !httpContext.Response.HasStarted)
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
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId && !u.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'utilisateur courant");
                return null;
            }
        }

        private async Task IncrementFailedLoginAsync(int userId)
        {
            try
            {
                // Utiliser ExecuteSqlRaw pour éviter les problèmes de tracking
                await _db.Database.ExecuteSqlRawAsync(
                    @"UPDATE SUser 
                      SET AccessFailedCount = AccessFailedCount + 1,
                          LockoutEnd = CASE WHEN AccessFailedCount >= 4 THEN DATEADD(minute, 30, GETUTCDATE()) ELSE LockoutEnd END
                      WHERE Id = {0}",
                    userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'incrémentation des échecs");
            }
        }

        private async Task UpdateLastLoginAsync(int userId)
        {
            try
            {
                // Utiliser ExecuteSqlRaw pour éviter les problèmes de tracking
                await _db.Database.ExecuteSqlRawAsync(
                    @"UPDATE SUser 
                      SET AccessFailedCount = 0, 
                          LockoutEnd = NULL, 
                          LastLoginDate = GETUTCDATE()
                      WHERE Id = {0}",
                    userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la dernière connexion");
            }
        }

        private bool VerifyPassword(string password, string storedValue)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedValue))
                return false;

            if (!IsBCryptHash(storedValue))
            {
                var legacyMatch = string.Equals(password, storedValue, StringComparison.Ordinal);
                if (legacyMatch)
                {
                    _logger.LogWarning("Mot de passe legacy détecté pour un utilisateur. Pensez à migrer vers BCrypt.");
                }

                return legacyMatch;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, storedValue);
            }
            catch (BCrypt.Net.SaltParseException ex)
            {
                _logger.LogWarning(ex, "Hash de mot de passe invalide, tentative de vérification legacy");
                return string.Equals(password, storedValue, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification du mot de passe");
                return false;
            }
        }

        private static bool IsBCryptHash(string hash)
        {
            return hash.StartsWith("$2a$", StringComparison.Ordinal) ||
                   hash.StartsWith("$2b$", StringComparison.Ordinal) ||
                   hash.StartsWith("$2y$", StringComparison.Ordinal);
        }
    }
}

//using System;
//using System.Linq;
//using System.Security.Claims;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Authentication;
//using Microsoft.AspNetCore.Authentication.Cookies;
//using Microsoft.AspNetCore.Http;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using Aion.Security;
//using Aion.DataEngine.Entities;
//using Aion.Domain.Contracts;

//namespace Aion.AppHost.Services
//{
//    /// <summary>
//    /// Service d'authentification Aion.
//    /// Corrigé pour éviter l'erreur "Headers are read-only".
//    /// </summary>
//    public class AuthService : IAuthService
//    {
//        private readonly SecurityDbContext _db;
//        private readonly IHttpContextAccessor _httpContext;
//        private readonly ILogger<AuthService> _logger;

//        public AuthService(
//            SecurityDbContext db,
//            IHttpContextAccessor httpContext,
//            ILogger<AuthService> logger)
//        {
//            _db = db;
//            _httpContext = httpContext;
//            _logger = logger;
//        }

//        public async Task<bool> LoginAsync(string username, string password, int tenantId, bool rememberMe = false)
//        {
//            try
//            {
//                _logger.LogInformation("🔐 Tentative de connexion : {Username} (Tenant: {TenantId})", username, tenantId);

//                // Vérifier le HttpContext
//                var httpContext = _httpContext.HttpContext;
//                if (httpContext == null)
//                {
//                    _logger.LogError("❌ HttpContext est null");
//                    return false;
//                }

//                // IMPORTANT : Vérifier que la réponse n'a pas déjà commencé
//                if (httpContext.Response.HasStarted)
//                {
//                    _logger.LogError("❌ La réponse HTTP a déjà commencé, impossible de se connecter");
//                    return false;
//                }

//                // Recherche de l'utilisateur
//                var user = await _db.SUser
//                    .AsNoTracking()
//                    .FirstOrDefaultAsync(u =>
//                        u.NormalizedUserName == username.ToUpperInvariant() &&
//                        u.TenantId == tenantId &&
//                        u.IsActive &&
//                        !u.Deleted);

//                if (user == null)
//                {
//                    _logger.LogWarning("⚠️ Utilisateur {Username} introuvable", username);
//                    return false;
//                }

//                // Vérification du mot de passe
//                if (!VerifyPassword(password, user.PasswordHash))
//                {
//                    _logger.LogWarning("⚠️ Mot de passe incorrect pour {Username}", username);

//                    // Incrémenter les échecs
//                    var userToUpdate = await _db.SUser.FindAsync(user.Id);
//                    if (userToUpdate != null)
//                    {
//                        userToUpdate.AccessFailedCount++;
//                        if (userToUpdate.AccessFailedCount >= 5)
//                        {
//                            userToUpdate.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
//                            _logger.LogWarning("🔒 Compte verrouillé : {Username}", username);
//                        }
//                        await _db.SaveChangesAsync();
//                    }

//                    return false;
//                }

//                // Vérification du lockout
//                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
//                {
//                    _logger.LogWarning("🔒 Compte verrouillé jusqu'à {LockoutEnd}", user.LockoutEnd);
//                    return false;
//                }

//                // Mise à jour de l'utilisateur (dernière connexion, reset échecs)
//                var userEntity = await _db.SUser.FindAsync(user.Id);
//                if (userEntity != null)
//                {
//                    userEntity.AccessFailedCount = 0;
//                    userEntity.LockoutEnd = null;
//                    userEntity.LastLoginDate = DateTime.UtcNow;
//                    await _db.SaveChangesAsync();
//                }

//                // Création des claims
//                var claims = new[]
//                {
//                    new Claim("sub", user.Id.ToString()),
//                    new Claim("tenant", user.TenantId.ToString()),
//                    new Claim(ClaimTypes.Name, user.UserName),
//                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
//                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
//                    new Claim("fullname", user.FullName ?? user.UserName)
//                };

//                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
//                var principal = new ClaimsPrincipal(identity);

//                var authProperties = new AuthenticationProperties
//                {
//                    IsPersistent = rememberMe,
//                    ExpiresUtc = rememberMe
//                        ? DateTimeOffset.UtcNow.AddDays(30)
//                        : DateTimeOffset.UtcNow.AddHours(8),
//                    AllowRefresh = true
//                };

//                // Connexion
//                await httpContext.SignInAsync(
//                    CookieAuthenticationDefaults.AuthenticationScheme,
//                    principal,
//                    authProperties);

//                _logger.LogInformation("✅ Connexion réussie : {Username} (ID: {UserId})", username, user.Id);
//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "❌ Erreur lors de la connexion");
//                return false;
//            }
//        }

//        public async Task LogoutAsync()
//        {
//            try
//            {
//                var httpContext = _httpContext.HttpContext;
//                if (httpContext != null && !httpContext.Response.HasStarted)
//                {
//                    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
//                    _logger.LogInformation("👋 Déconnexion réussie");
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "❌ Erreur lors de la déconnexion");
//            }
//        }

//        public async Task<SUser?> GetCurrentUserAsync()
//        {
//            try
//            {
//                var httpContext = _httpContext.HttpContext;
//                if (httpContext?.User == null) return null;

//                var userIdClaim = httpContext.User.FindFirst("sub");
//                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
//                    return null;

//                return await _db.SUser
//                    .AsNoTracking()
//                    .FirstOrDefaultAsync(u => u.Id == userId && !u.Deleted);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "❌ Erreur GetCurrentUser");
//                return null;
//            }
//        }

//        private bool VerifyPassword(string password, string hash)
//        {
//            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
//                return false;

//            // TEMPORAIRE : comparaison simple
//            // TODO : Implémenter BCrypt en production
//            return password == hash;
//        }
//    }
//}
////using Aion.DataEngine.Entities;
// //using Aion.Domain.Contracts;
// //using Aion.Security;
// //using Microsoft.AspNetCore.Authentication;
// //using Microsoft.AspNetCore.Authentication.Cookies;
// //using Microsoft.EntityFrameworkCore;
// //using System.Security.Claims;

////namespace Aion.AppHost.Services
////{

////    /// <summary>
////    /// Service d'authentification Aion.
////    /// Gère le login/logout et la création des claims.
////    /// </summary>
////    public class AuthService : IAuthService
////    {
////        private readonly SecurityDbContext _db;
////        private readonly IHttpContextAccessor _httpContext;
////        private readonly ILogger<AuthService> _logger;

////        public AuthService(
////            SecurityDbContext db,
////            IHttpContextAccessor httpContext,
////            ILogger<AuthService> logger)
////        {
////            _db = db;
////            _httpContext = httpContext;
////            _logger = logger;
////        }

////        public async Task<bool> LoginAsync(string username, string password, int tenantId, bool rememberMe = false)
////        {
////            try
////            {
////                _logger.LogInformation("Tentative de connexion pour {Username} (Tenant: {TenantId})", username, tenantId);

////                // Recherche de l'utilisateur
////                var user = await _db.SUser
////                    .FirstOrDefaultAsync(u =>
////                        u.NormalizedUserName == username.ToUpperInvariant() &&
////                        u.TenantId == tenantId &&
////                        u.IsActive &&
////                        !u.Deleted);

////                if (user == null)
////                {
////                    _logger.LogWarning("Utilisateur {Username} introuvable ou inactif", username);
////                    return false;
////                }

////                // Vérification du mot de passe
////                if (!VerifyPassword(password, user.PasswordHash))
////                {
////                    _logger.LogWarning("Mot de passe incorrect pour {Username}", username);

////                    user.AccessFailedCount++;
////                    if (user.AccessFailedCount >= 5)
////                    {
////                        user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
////                        _logger.LogWarning("Compte {Username} verrouillé après 5 échecs", username);
////                    }

////                    await _db.SaveChangesAsync();
////                    return false;
////                }

////                // Vérification du lockout
////                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
////                {
////                    _logger.LogWarning("Compte {Username} verrouillé jusqu'à {LockoutEnd}", username, user.LockoutEnd);
////                    return false;
////                }

////                // Reset des échecs et mise à jour dernière connexion
////                user.AccessFailedCount = 0;
////                user.LockoutEnd = null;
////                user.LastLoginDate = DateTime.UtcNow;
////                await _db.SaveChangesAsync();

////                // Création des claims de base
////                var claims = new[]
////                {
////                    new Claim("sub", user.Id.ToString()),
////                    new Claim("tenant", user.TenantId.ToString()),
////                    new Claim(ClaimTypes.Name, user.UserName),
////                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
////                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
////                    new Claim("fullname", user.FullName ?? user.UserName)
////                };

////                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
////                var principal = new ClaimsPrincipal(identity);

////                var authProperties = new AuthenticationProperties
////                {
////                    IsPersistent = rememberMe,
////                    ExpiresUtc = rememberMe
////                        ? DateTimeOffset.UtcNow.AddDays(30)
////                        : DateTimeOffset.UtcNow.AddHours(8),
////                    AllowRefresh = true
////                };

////                var httpContext = _httpContext.HttpContext;
////                if (httpContext == null)
////                {
////                    _logger.LogError("HttpContext est null lors du login");
////                    return false;
////                }

////                await httpContext.SignInAsync(
////                    CookieAuthenticationDefaults.AuthenticationScheme,
////                    principal,
////                    authProperties);

////                _logger.LogInformation("✅ Connexion réussie pour {Username} (ID: {UserId})", username, user.Id);
////                return true;
////            }
////            catch (Exception ex)
////            {
////                _logger.LogError(ex, "❌ Erreur lors de la connexion de {Username}", username);
////                return false;
////            }
////        }

////        public async Task LogoutAsync()
////        {
////            try
////            {
////                var httpContext = _httpContext.HttpContext;
////                if (httpContext != null)
////                {
////                    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
////                    _logger.LogInformation("Déconnexion réussie");
////                }
////            }
////            catch (Exception ex)
////            {
////                _logger.LogError(ex, "Erreur lors de la déconnexion");
////            }
////        }

////        public async Task<SUser?> GetCurrentUserAsync()
////        {
////            try
////            {
////                var httpContext = _httpContext.HttpContext;
////                if (httpContext == null) return null;

////                var userIdClaim = httpContext.User.FindFirst("sub");
////                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
////                    return null;

////                return await _db.SUser
////                    .FirstOrDefaultAsync(u => u.Id == userId && !u.Deleted);
////            }
////            catch (Exception ex)
////            {
////                _logger.LogError(ex, "Erreur lors de la récupération de l'utilisateur courant");
////                return null;
////            }
////        }

////        private bool VerifyPassword(string password, string hash)
////        {
////            // TEMPORAIRE : comparaison simple pour le développement
////            // EN PRODUCTION : utiliser BCrypt.Net-Next
////            // return BCrypt.Net.BCrypt.Verify(password, hash);

////            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
////                return false;

////            return password == hash;
////        }
////    }
////}