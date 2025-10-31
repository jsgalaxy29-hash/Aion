using Aion.DataEngine.Entities;
using Aion.Domain.Contracts;
using Aion.Domain.UI;
using Aion.Security.Services;
using Microsoft.EntityFrameworkCore;

namespace Aion.Infrastructure.Services
{


    /// <summary>
    /// Implémentation du provider de menus avec filtrage RBAC.
    /// </summary>
    public class MenuProvider : IMenuProvider
    {
        private readonly AionDbContext _db;
        private readonly IRightService _rightService;

        public MenuProvider(AionDbContext db, IRightService rightService)
        {
            _db = db;
            _rightService = rightService;
        }

        public async Task<IReadOnlyList<SMenu>> GetAuthorizedMenuAsync(int tenantId, int userId, CancellationToken ct)
        {
            // Récupération des IDs de menus autorisés
            var authorizedIds = await _rightService.GetAuthorizedMenuIdsAsync(userId, tenantId, ct);

            // Si aucun droit, retour vide
            if (!authorizedIds.Any())
                return new List<SMenu>();


            // Récupération des menus depuis la base (table S_Menu ou équivalent)
            // ADAPTATION : Remplacer par votre table réelle
            var menus = await _db.Set<SMenu>() // Supposé que MenuEntity est mappé
                .Where(m => m.Actif && authorizedIds.Contains(m.Id))
                .OrderBy(m => m.Order)
                .ToListAsync(ct);

            return menus;
        }

        public async Task<IReadOnlyList<SMenu>> GetAllMenuAsync(int tenantId, CancellationToken ct)
        {
            // ADAPTATION : Ajouter filtre TenantId si multi-tenant
            var menus = await _db.Set<SMenu>()
                .Where(m => m.Actif)
                .OrderBy(m => m.Order)
                .ToListAsync(ct);

            return menus;
        }
    }
}