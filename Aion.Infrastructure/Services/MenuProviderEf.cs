using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.Contracts;
using Aion.Domain.UI;
using Microsoft.EntityFrameworkCore;

namespace Aion.Infrastructure.Services
{
    /// <summary>
    /// Fournit la hiérarchie des menus autorisés pour un utilisateur en filtrant via IRightsService.
    /// </summary>
    public sealed class MenuProviderEf : IMenuProvider
    {
        private readonly AionDbContext _db;
        private readonly IRightsService _rights;

        public MenuProviderEf(AionDbContext db, IRightsService rights)
        {
            _db = db;
            _rights = rights;
        }

        public async Task<IReadOnlyList<MenuEntity>> GetAuthorizedMenuAsync(Guid tenantId, Guid userId, CancellationToken ct)
        {
            var authorizedIds = await _rights.GetAuthorizedMenuIdsAsync(tenantId, userId, ct);

            // On récupère tous les menus du tenant
            var menus = await _db.S_Menu
                .Where(m => m.TenantId == tenantId)
                .OrderBy(m => m.ModuleId).ThenBy(m => m.ParentId).ThenBy(m => m.Order)
                .ToListAsync(ct);

            // Garder les feuilles autorisées et leurs ancêtres
            var keep = new HashSet<int>(authorizedIds);
            bool added;
            do
            {
                added = false;
                foreach (var m in menus.Where(x => x.ParentId.HasValue))
                {
                    if (keep.Contains(m.Id) && m.ParentId.HasValue && !keep.Contains(m.ParentId.Value))
                    {
                        keep.Add(m.ParentId.Value);
                        added = true;
                    }
                }
            } while (added);

            // Filtrer et retourner
            return menus.Where(m => keep.Contains(m.Id)).ToList();
        }
    }
}
