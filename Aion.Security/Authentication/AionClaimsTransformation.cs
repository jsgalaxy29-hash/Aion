using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Aion.Security.Services;

namespace Aion.Security.Authentication
{
    /// <summary>
    /// Transforme les claims de l'utilisateur authentifié en ajoutant les droits.
    /// Exécuté automatiquement après l'authentification.
    /// </summary>
    public class AionClaimsTransformation : IClaimsTransformation
    {
        private readonly IRightService _rightService;

        public AionClaimsTransformation(IRightService rightService)
        {
            _rightService = rightService;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // Si déjà transformé, retour immédiat
            if (principal.HasClaim(c => c.Type == "AionRightsLoaded"))
                return principal;

            var userIdClaim = principal.FindFirst("sub") ?? principal.FindFirst(ClaimTypes.NameIdentifier);
            var tenantIdClaim = principal.FindFirst("tenant");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return principal;

            if (tenantIdClaim == null || !int.TryParse(tenantIdClaim.Value, out var tenantId))
                return principal;

            // Chargement des droits utilisateur
            var rights = await _rightService.GetUserRightsAsync(userId, tenantId);

            var identity = (ClaimsIdentity)principal.Identity!;

            // Ajout des claims de droits
            foreach (var targetGroup in rights)
            {
                foreach (var right in targetGroup.Value)
                {
                    if (right.Right1) identity.AddClaim(new Claim("Right", $"{targetGroup.Key}:{right.SubjectId}:R1"));
                    if (right.Right2) identity.AddClaim(new Claim("Right", $"{targetGroup.Key}:{right.SubjectId}:R2"));
                    if (right.Right3) identity.AddClaim(new Claim("Right", $"{targetGroup.Key}:{right.SubjectId}:R3"));
                    if (right.Right4) identity.AddClaim(new Claim("Right", $"{targetGroup.Key}:{right.SubjectId}:R4"));
                    if (right.Right5) identity.AddClaim(new Claim("Right", $"{targetGroup.Key}:{right.SubjectId}:R5"));
                }
            }

            // Marqueur de transformation effectuée
            identity.AddClaim(new Claim("AionRightsLoaded", "true"));

            return principal;
        }
    }
}