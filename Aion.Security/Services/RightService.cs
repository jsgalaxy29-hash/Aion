using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Aion.Security.Models;
using Aion.DataEngine.Entities;

namespace Aion.Security.Services
{
    /// <summary>
    /// Implémentation du service de droits avec cache mémoire.
    /// Règle de fusion : true > false (si un groupe accorde le droit, il est accordé).
    /// </summary>
    public class RightService : IRightService
    {
        private readonly SecurityDbContext _db;
        private readonly IMemoryCache _cache;
        private const string CacheKeyPrefix = "UserRights_";
        private const int CacheExpirationMinutes = 30;

        public RightService(SecurityDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<bool> HasRightAsync(int userId, int tenantId, string target, int subjectId, RightFlag flag, CancellationToken ct = default)
        {
            var rights = await GetUserRightsAsync(userId, tenantId, ct);

            if (!rights.TryGetValue(target, out var targetRights))
                return false;

            var right = targetRights.FirstOrDefault(r => r.SubjectId == subjectId);
            return right?.HasRight(flag) ?? false;
        }

        public async Task<Dictionary<string, List<UserRights>>> GetUserRightsAsync(int userId, int tenantId, CancellationToken ct = default)
        {
            var cacheKey = $"{CacheKeyPrefix}{userId}_{tenantId}";

            if (_cache.TryGetValue<Dictionary<string, List<UserRights>>>(cacheKey, out var cached))
                return cached!;

            // Récupération des groupes actifs de l'utilisateur
            var groupIds = await _db.SUserGroup
                .Where(ug => ug.UserId == userId && ug.IsLinkActive && !ug.Deleted && ug.TenantId == tenantId)
                .Select(ug => ug.GroupId)
                .ToListAsync(ct);

            if (!groupIds.Any())
                return new Dictionary<string, List<UserRights>>();

            // Récupération de tous les droits des groupes
            var rights = await _db.SRight
                .Where(r => groupIds.Contains(r.GroupId) && !r.Deleted && r.TenantId == tenantId)
                .ToListAsync(ct);

            // Agrégation par Target + SubjectId avec règle true > false
            var aggregated = rights
                .GroupBy(r => new { r.Target, r.SubjectId })
                .Select(g => new UserRights
                {
                    Target = g.Key.Target,
                    SubjectId = g.Key.SubjectId,
                    // true > false : si au moins un groupe accorde le droit, il est accordé
                    Right1 = g.Any(r => r.Right1 == true),
                    Right2 = g.Any(r => r.Right2 == true),
                    Right3 = g.Any(r => r.Right3 == true),
                    Right4 = g.Any(r => r.Right4 == true),
                    Right5 = g.Any(r => r.Right5 == true)
                })
                .GroupBy(r => r.Target)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Mise en cache
            _cache.Set(cacheKey, aggregated, TimeSpan.FromMinutes(CacheExpirationMinutes));

            return aggregated;
        }

        public async Task<List<int>> GetAuthorizedMenuIdsAsync(int userId, int tenantId, CancellationToken ct = default)
        {
            var rights = await GetUserRightsAsync(userId, tenantId, ct);

            if (!rights.TryGetValue("Menu", out var menuRights))
                return new List<int>();

            // Un menu est autorisé si Right1 (Voir/Accéder) est à true
            return menuRights
                .Where(r => r.Right1)
                .Select(r => r.SubjectId)
                .ToList();
        }

        public void InvalidateCache(int userId)
        {
            // Invalider pour tous les tenants (pattern approximatif)
            // En production, utiliser IMemoryCache avec tags ou Redis
            _cache.Remove($"{CacheKeyPrefix}{userId}");
        }
    }
}