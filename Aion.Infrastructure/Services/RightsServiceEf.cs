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
        private readonly AionDbContext _db;
        public RightsServiceEf(AionDbContext db) => _db = db;

        public async Task<bool> IsMenuAuthorizedAsync(int tenantId, int userId, string menu, CancellationToken ct)
        {
            var groupIds = await _db.SUserGroup
                .Where(x => x.TenantId == tenantId && x.UserId == userId)
                .Select(x => x.GroupId)
                .ToListAsync(ct);

            if (groupIds.Count == 0) return false;

            var menuTypeId = await _db.SRightType
                .Where(t => (t.TenantId == tenantId || t.TenantId ==0) && t.Code == "Menu")
                .Select(t => t.Id)
                .FirstAsync(ct);

            var allowed = await _db.SRight.AnyAsync(d =>
                d.TenantId == tenantId &&
                d.SubjectId == menuTypeId &&
                d.Target == menu &&
                groupIds.Contains(d.GroupId) &&
                d.Right1.GetValueOrDefault(false), ct);

            return allowed;
        }

        public async Task<HashSet<string>> GetAuthorizedMenuIdsAsync(int tenantId, int userId, CancellationToken ct)
        {
            var groupIds = await _db.SUserGroup
                .Where(x => x.TenantId == tenantId && x.UserId == userId)
                .Select(x => x.GroupId)
                .ToListAsync(ct);

            if (groupIds.Count == 0) return new HashSet<string>();

            var menuTypeId = await _db.SRightType
                .Where(t => (t.TenantId == tenantId || t.TenantId == 0) && t.Code == "Menu")
                .Select(t => t.Id)
                .FirstAsync(ct);

            var ids = await _db.SRight 
                .Where(d => d.TenantId == tenantId && d.SubjectId == menuTypeId && d.Right1.GetValueOrDefault(false) && groupIds.Contains(d.GroupId) && d.Target != null)
                .Select(d => d.Target)
                .Distinct()
                .ToListAsync(ct);

            return ids.ToHashSet();
        }

    }
}
