using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.Contracts;
using Aion.Domain.UI.State;
using Aion.DataEngine.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aion.Infrastructure.Services
{
    /// <summary>
    /// Service de gestion du layout du dashboard utilisateur. Persisté via Entity Framework.
    /// </summary>
    public sealed class DashboardServiceEf : IUserDashboardService
    {
        private readonly IDbContextFactory<AionDbContext> _dbFactory;

        public DashboardServiceEf(IDbContextFactory<AionDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<IReadOnlyList<UserDashboardLayoutEntity>> GetLayoutAsync(int tenantId, int userId, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var layouts = await db.UserDashboardLayouts
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.UserId == userId)
                .OrderBy(x => x.Y).ThenBy(x => x.X)
                .Select(x => new UserDashboardLayoutEntity
                {
                    Id = x.Id,
                    TenantId = x.TenantId,
                    UserId = x.UserId,
                    WidgetCode = x.WidgetCode,
                    X = x.X,
                    Y = x.Y,
                    W = x.W,
                    H = x.H,
                    SettingsJson = x.SettingsJson
                })
                .ToListAsync(ct);
            return layouts;
        }

        public async Task SaveLayoutAsync(int tenantId, int userId, IEnumerable<UserDashboardLayoutEntity> layout, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            // Supprimer les dispositions existantes
            var existing = await db.UserDashboardLayouts
                .Where(x => x.TenantId == tenantId && x.UserId == userId)
                .ToListAsync(ct);
            db.UserDashboardLayouts.RemoveRange(existing);

            var utcNow = DateTime.UtcNow;
            var entities = layout.Select(item => new UUserDashboardLayout
            {
                TenantId = tenantId,
                UserId = userId,
                WidgetCode = item.WidgetCode,
                X = item.X,
                Y = item.Y,
                W = item.W,
                H = item.H,
                SettingsJson = item.SettingsJson,
                Actif = true,
                Doc = false,
                Deleted = false,
                DtCreation = utcNow,
                DtModification = utcNow,
                UsrCreationId = userId,
                UsrModificationId = userId
            }).ToList();

            await db.UserDashboardLayouts.AddRangeAsync(entities, ct);
            await db.SaveChangesAsync(ct);
        }
    }
}
