using Aion.Domain.Contracts;
using Aion.Domain.UI;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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

        public async Task<IReadOnlyList<MenuEntity>> GetAuthorizedMenuAsync(int tenantId, int userId, CancellationToken ct)
        {
            var authorizedIds = await _rights.GetAuthorizedMenuIdsAsync(tenantId, userId, ct);

            // On récupère tous les menus du tenant
            var menus = await _db.SMenu
                .Where(m => m.TenantId == tenantId)
                .OrderBy(m => m.ModuleId).ThenBy(m => m.ParentId).ThenBy(m => m.Order)
                .ToListAsync(ct);

            // Garder les feuilles autorisées et leurs ancêtres
            var keep = new HashSet<string>(authorizedIds);
            bool added;
            do
            {
                added = false;
                foreach (var m in menus.Where(x => x.ParentId != 0))
                {
                    if (keep.Contains(m.Id.ToString()) && !keep.Contains(m.ParentId.ToString()))
                    {
                        keep.Add(m.ParentId.ToString());
                        added = true;
                    }
                }
            } while (added);

            // Filtrer et retourner
            return (IReadOnlyList<MenuEntity>)menus.Where(m => keep.Contains(m.Id.ToString())).ToList();
        }
    }
}
