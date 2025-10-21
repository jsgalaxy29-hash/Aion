using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.Contracts;
using Aion.Domain.UI.State;
using Microsoft.EntityFrameworkCore;

namespace Aion.Infrastructure.Services
{
    /// <summary>
    /// Service de gestion du layout du dashboard utilisateur. Persisté via Entity Framework.
    /// </summary>
    public sealed class DashboardServiceEf : IUserDashboardService
    {
        private readonly AionDbContext _db;

        public DashboardServiceEf(AionDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<UserDashboardLayoutEntity>> GetLayoutAsync(Guid tenantId, Guid userId, CancellationToken ct)
        {
            var layouts = await _db.U_UserDashboardLayout
                .Where(x => x.TenantId == tenantId && x.UserId == userId)
                .OrderBy(x => x.Y).ThenBy(x => x.X)
                .ToListAsync(ct);
            return layouts;
        }

        public async Task SaveLayoutAsync(Guid tenantId, Guid userId, IEnumerable<UserDashboardLayoutEntity> layout, CancellationToken ct)
        {
            // Supprimer les dispositions existantes
            var existing = await _db.U_UserDashboardLayout
                .Where(x => x.TenantId == tenantId && x.UserId == userId)
                .ToListAsync(ct);
            _db.U_UserDashboardLayout.RemoveRange(existing);

            // Ajouter la nouvelle disposition
            foreach (var item in layout)
            {
                item.TenantId = tenantId;
                item.UserId = userId;
            }
            await _db.U_UserDashboardLayout.AddRangeAsync(layout, ct);
            await _db.SaveChangesAsync(ct);
        }
    }
}
