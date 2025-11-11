using Aion.DataEngine.Entities;
using Aion.Domain.Contracts;
using Aion.Domain.UI;
using Aion.Security.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Aion.Infrastructure.Services
{


    /// <summary>
    /// Implémentation du provider de menus avec filtrage RBAC.
    /// </summary>
    public class MenuProvider : IMenuProvider
    {
        private readonly IDbContextFactory<AionDbContext> _dbFactory;
        private readonly IRightService _rightService;

        public MenuProvider(IDbContextFactory<AionDbContext> dbFactory, IRightService rightService)
        {
            _dbFactory = dbFactory;
            _rightService = rightService;
        }

        public async Task<IReadOnlyList<SMenu>> GetAuthorizedMenuAsync(int tenantId, int userId, CancellationToken ct)
        {
            var authorizedIds = await _rightService.GetAuthorizedMenuIdsAsync(userId, tenantId, ct);
            var authorizedSet = authorizedIds.Count > 0
                ? authorizedIds.ToHashSet()
                : new HashSet<int>();

            if (authorizedSet.Count == 0)
                return new List<SMenu>();


            // Récupération des menus depuis la base (table S_Menu ou équivalent)
            // ADAPTATION : Remplacer par votre table réelle
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var menus = await db.Set<SMenu>()
                .Include(m => m.Module)
                .Where(m => m.TenantId == tenantId && m.Actif && authorizedSet.Contains(m.Id))
                .OrderBy(m => m.Order)
                .ToListAsync(ct);

            if (!menus.Any())
            {
                return menus;
            }

            // Ajoute automatiquement les parents nécessaires pour afficher la hiérarchie
            var parentIds = menus
                .Where(m => m.ParentId.HasValue)
                .Select(m => m.ParentId!.Value)
                .Distinct()
                .Except(menus.Select(m => m.Id))
                .ToList();

            while (parentIds.Count > 0)
            {
                var parents = await db.Set<SMenu>()
                    .Include(m => m.Module)
                    .Where(m => parentIds.Contains(m.Id) && m.TenantId == tenantId)
                    .ToListAsync(ct);

                if (!parents.Any())
                {
                    break;
                }

                menus.AddRange(parents);

                parentIds = parents
                    .Where(m => m.ParentId.HasValue)
                    .Select(m => m.ParentId!.Value)
                    .Distinct()
                    .Except(menus.Select(m => m.Id))
                    .ToList();
            }

            return menus
                .DistinctBy(m => m.Id)
                .OrderBy(m => m.Order)
                .ToList();
        }

        public async Task<IReadOnlyList<SMenu>> GetAllMenuAsync(int tenantId, CancellationToken ct)
        {
            // ADAPTATION : Ajouter filtre TenantId si multi-tenant
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var menus = await db.Set<SMenu>()
                .Include(m => m.Module)
                .Where(m => m.TenantId == tenantId && m.Actif)
                .OrderBy(m => m.Order)
                .ToListAsync(ct);

            return menus;
        }
    }
}