using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Aion.Security;

namespace Aion.AppHost.Pages
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly IDbContextFactory<SecurityDbContext> _dbFactory;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(IDbContextFactory<SecurityDbContext> dbFactory, ILogger<LoginModel> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            public string Username { get; set; } = "admin";

            [Required]
            public string Password { get; set; } = "admin";

            [Required]
            [Range(1, int.MaxValue)]
            public int TenantId { get; set; } = 1;

            public bool RememberMe { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Veuillez remplir tous les champs.";
                return Page();
            }

            try
            {
                _logger.LogInformation("Tentative de connexion pour {Username}", Input.Username);

                // Recherche de l'utilisateur
                await using var db = await _dbFactory.CreateDbContextAsync();

                var user = await db.SUser
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u =>
                        u.NormalizedUserName == Input.Username.ToUpperInvariant() &&
                        u.TenantId == Input.TenantId &&
                        u.IsActive &&
                        !u.Deleted);

                if (user == null)
                {
                    _logger.LogWarning("Utilisateur {Username} introuvable", Input.Username);
                    ErrorMessage = "Identifiants incorrects.";
                    return Page();
                }

                // Vérification du mot de passe
                if (!VerifyPassword(Input.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Mot de passe incorrect pour {Username}", Input.Username);

                    // Incrémenter les échecs
                    await IncrementFailedLoginAsync(user.Id);

                    ErrorMessage = "Identifiants incorrects.";
                    return Page();
                }

                // Vérification du lockout
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    _logger.LogWarning("Compte {Username} verrouillé", Input.Username);
                    ErrorMessage = $"Compte verrouillé jusqu'à {user.LockoutEnd.Value.ToLocalTime():HH:mm}";
                    return Page();
                }

                // Création des claims
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
                    IsPersistent = Input.RememberMe,
                    ExpiresUtc = Input.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(30)
                        : DateTimeOffset.UtcNow.AddHours(8),
                    AllowRefresh = true,
                    IssuedUtc = DateTimeOffset.UtcNow
                };

                // Authentification
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    authProperties);

                _logger.LogInformation("✅ Connexion réussie pour {Username}", Input.Username);

                // Mise à jour dernière connexion
                await UpdateLastLoginAsync(user.Id);

                // Redirection
                return LocalRedirect("/Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la connexion");
                ErrorMessage = "Une erreur s'est produite. Veuillez réessayer.";
                return Page();
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch (BCrypt.Net.SaltParseException ex)
            {
                _logger.LogError(ex, "Hash de mot de passe invalide");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification du mot de passe");
                return false;
            }
        }

        private async Task IncrementFailedLoginAsync(int userId)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync();

                var now = DateTime.UtcNow;
                var lockoutEnd = now.AddMinutes(30);
                FormattableString sql = $@"
                    UPDATE [SUser]
                    SET [AccessFailedCount] = [AccessFailedCount] + 1,
                        [LockoutEnd] = CASE WHEN [AccessFailedCount] + 1 >= 4 THEN {lockoutEnd} ELSE [LockoutEnd] END,
                        [DtModification] = {now}
                    WHERE [Id] = {userId};";


                var rows = await db.Database.ExecuteSqlInterpolatedAsync(sql);

                if (rows == 0)
                {
                    _logger.LogWarning("Utilisateur introuvable pour l'incrémentation des tentatives échouées : {UserId}", userId);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur incrémentation échecs");
            }
        }

        private async Task UpdateLastLoginAsync(int userId)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync();

                var user = await db.SUser.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return;
                }

                user.AccessFailedCount = 0;
                user.LockoutEnd = null;
                user.LastLoginDate = DateTime.UtcNow;
                user.DtModification = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur mise à jour dernière connexion");
            }
        }
    }
}