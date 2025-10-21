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

        public async Task<bool> IsMenuAuthorizedAsync(Guid tenantId, Guid userId, int menuId, CancellationToken ct)
        {
            var groupIds = await _db.S_Groupe_User
                .Where(x => x.TenantId == tenantId && x.UserId == userId)
                .Select(x => x.GroupeId)
                .ToListAsync(ct);

            if (groupIds.Count == 0) return false;

            var menuTypeId = await _db.S_Droit_Type
                .Where(t => (t.TenantId == tenantId || t.TenantId == Guid.Empty) && t.Code == "Menu")
                .Select(t => t.Id)
                .FirstAsync(ct);

            var allowed = await _db.S_Droit.AnyAsync(d =>
                d.TenantId == tenantId &&
                d.DroitTypeId == menuTypeId &&
                d.TargetId == menuId &&
                groupIds.Contains(d.GroupeId) &&
                d.Droit1, ct);

            return allowed;
        }

        public async Task<HashSet<int>> GetAuthorizedMenuIdsAsync(Guid tenantId, Guid userId, CancellationToken ct)
        {
            var groupIds = await _db.S_Groupe_User
                .Where(x => x.TenantId == tenantId && x.UserId == userId)
                .Select(x => x.GroupeId)
                .ToListAsync(ct);

            if (groupIds.Count == 0) return new HashSet<int>();

            var menuTypeId = await _db.S_Droit_Type
                .Where(t => (t.TenantId == tenantId || t.TenantId == Guid.Empty) && t.Code == "Menu")
                .Select(t => t.Id)
                .FirstAsync(ct);

            var ids = await _db.S_Droit
                .Where(d => d.TenantId == tenantId && d.DroitTypeId == menuTypeId && d.Droit1 && groupIds.Contains(d.GroupeId) && d.TargetId != null)
                .Select(d => d.TargetId!.Value)
                .Distinct()
                .ToListAsync(ct);

            return ids.ToHashSet();
        }
    }
}
