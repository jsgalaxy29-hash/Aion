using System;
using System.Linq;
using System.Security.Claims;
using Aion.Security.Models;

namespace Aion.Security.Extensions
{
    /// <summary>
    /// Extensions pour simplifier la manipulation des claims et droits.
    /// </summary>
    public static class SecurityExtensions
    {
        /// <summary>
        /// Récupère l'ID utilisateur depuis les claims.
        /// </summary>
        public static int? GetUserId(this ClaimsPrincipal principal)
        {
            var claim = principal.FindFirst("sub") ?? principal.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }

        /// <summary>
        /// Récupère le TenantId depuis les claims.
        /// </summary>
        public static int? GetTenantId(this ClaimsPrincipal principal)
        {
            if (principal != null)
            {
                var claim = principal.FindFirst("tenant");
                return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
            } else
            {
                return null;
            }
        }

        /// <summary>
        /// Vérifie si l'utilisateur possède un droit spécifique dans ses claims.
        /// Format attendu : "Right:Target:SubjectId:R1" (ex: "Menu:5:R1")
        /// </summary>
        public static bool HasRightClaim(this ClaimsPrincipal principal, string target, int subjectId, RightFlag flag)
        {
            var flagCode = flag switch
            {
                RightFlag.Right1 => "R1",
                RightFlag.Right2 => "R2",
                RightFlag.Right3 => "R3",
                RightFlag.Right4 => "R4",
                RightFlag.Right5 => "R5",
                _ => throw new ArgumentException("Invalid right flag")
            };

            var expectedClaim = $"{target}:{subjectId}:{flagCode}";
            return principal.Claims.Any(c => c.Type == "Right" && c.Value == expectedClaim);
        }

        /// <summary>
        /// Récupère le nom complet de l'utilisateur.
        /// </summary>
        public static string GetFullName(this ClaimsPrincipal principal)
        {
            return principal.FindFirst("fullname")?.Value
                ?? principal.FindFirst(ClaimTypes.Name)?.Value
                ?? "Utilisateur";
        }

        /// <summary>
        /// Récupère l'email de l'utilisateur.
        /// </summary>
        public static string? GetEmail(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Email)?.Value;
        }

        /// <summary>
        /// Vérifie si l'utilisateur appartient au groupe Administrateurs.
        /// </summary>
        public static bool IsAdmin(this ClaimsPrincipal principal)
        {
            return principal.HasClaim("group", "Administrateurs");
        }
    }
}