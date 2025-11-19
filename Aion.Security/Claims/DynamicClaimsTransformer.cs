using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Aion.Security.Claims
{
    public sealed class DynamicClaimsTransformer : IClaimsTransformation
    {
        private readonly IDbContextFactory<SecurityDbContext> _dbFactory;
        public DynamicClaimsTransformer(IDbContextFactory<SecurityDbContext> dbFactory) => _dbFactory = dbFactory;

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var id = principal.Identity as ClaimsIdentity;
            if (id == null || !id.IsAuthenticated) return principal;
            var name = id.Name;
            if (string.IsNullOrWhiteSpace(name)) return principal;

            // TODO: adjust mapping to your user table; example relies on ApplicationUser by UserName
            await using var db = await _dbFactory.CreateDbContextAsync();

            var user = await db.SUser
                .AsNoTracking()
                .Where(u => u.UserName == name && !u.Deleted)
                .Select(u => new { u.Id, u.TenantId })
                .FirstOrDefaultAsync();
            if (user == null) return principal;

            var groupIds = await db.SUserGroup
                .AsNoTracking()
                .Where(g => g.UserId == user.Id && g.TenantId == user.TenantId && g.IsLinkActive && !g.Deleted)
                .Select(g => g.GroupId)
                .ToArrayAsync();

            if (groupIds.Length == 0) return principal;

            // Example: build rights from SRight / SRightType for the current tenant
            var rights = await db.SRight
                .AsNoTracking()
                .Where(r => groupIds.Contains(r.GroupId) && r.TenantId == user.TenantId && !r.Deleted)
                .Join(db.SRightType.AsNoTracking().Where(t => t.TenantId == user.TenantId && t.IsActive),
                    r => r.Target,
                    t => t.Code,
                    (r, t) => new { Right = r, TypeCode = t.Code })
                .ToListAsync();

            foreach (var x in rights)
            {
                if (x.Right.Right1 == true) id.AddClaim(new Claim("Right", $"{x.TypeCode}:Lecture:{x.Right.SubjectId}"));
                if (x.Right.Right2 == true) id.AddClaim(new Claim("Right", $"{x.TypeCode}:Ecriture:{x.Right.SubjectId}"));
                if (x.Right.Right3 == true) id.AddClaim(new Claim("Right", $"{x.TypeCode}:Suppression:{x.Right.SubjectId}"));
            }
            return principal;
        }
    }
}
