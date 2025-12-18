using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.Contracts;
using Aion.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace Aion.Infrastructure.Services
{
    /// <summary>
    /// Implémentation de IRightsService basée sur Entity Framework. Permet de déterminer si un menu est autorisé.
    /// </summary>
    public sealed class RightsServiceEf : IRightsService
    {
        private readonly IDbContextFactory<AionDbContext> _dbFactory;
        public RightsServiceEf(IDbContextFactory<AionDbContext> dbFactory) => _dbFactory = dbFactory;

        public async Task<bool> IsMenuAuthorizedAsync(int tenantId, int userId, int menuId, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var groupIds = await db.SUserGroup
                .Where(x => x.TenantId == tenantId && x.UserId == userId)
                .Select(x => x.GroupId)
                .ToListAsync(ct);

            if (groupIds.Count == 0) return false;

            var allowed = await db.SRight.AnyAsync(d =>
                d.TenantId == tenantId &&
                d.Target == "Menu" &&
                d.SubjectId == menuId &&
                groupIds.Contains(d.GroupId) &&
                d.Right1 == true, ct);

            return allowed;
        }

        public async Task<HashSet<int>> GetAuthorizedMenuIdsAsync(int tenantId, int userId, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var groupIds = await db.SUserGroup
                .Where(x => x.TenantId == tenantId && x.UserId == userId)
                .Select(x => x.GroupId)
                .ToListAsync(ct);

            if (groupIds.Count == 0) return new HashSet<int>();

            var ids = await db.SRight
                .Where(d => d.TenantId == tenantId && d.Target == "Menu" && d.Right1 == true && groupIds.Contains(d.GroupId))
                .Select(d => d.SubjectId)
                .Distinct()
                .ToListAsync(ct);

            return ids.ToHashSet();
        }

    }
}
