using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Aion.Security.Claims
{
    public sealed class DynamicClaimsTransformer : IClaimsTransformation
    {
        private readonly SecurityDbContext _db;
        public DynamicClaimsTransformer(SecurityDbContext db) => _db = db;

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var id = principal.Identity as ClaimsIdentity;
            if (id == null || !id.IsAuthenticated) return principal;
            var name = id.Name;
            if (string.IsNullOrWhiteSpace(name)) return principal;

            // TODO: adjust mapping to your user table; example relies on ApplicationUser by UserName
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == name);
            if (user == null) return principal;

            // Example: build rights from S_DROIT / S_DROIT_TYPE
            var rights = await _db.SRight.Join(_db.SRightType, r => r.SubjectId, t => t.Id, (r,t) => new { r, t })
                .Where(x => user.Groups.Any(g => g.GroupId == x.r.GroupId)) // replace with your actual linkage
                .ToListAsync();

            foreach (var x in rights)
            {
                if (x.r.Right1 == true) id.AddClaim(new Claim("Right", $"{x.t.Code}:Lecture:{x.r.SubjectId}"));
                if (x.r.Right2 == true) id.AddClaim(new Claim("Right", $"{x.t.Code}:Ecriture:{x.r.SubjectId}"));
                if (x.r.Right3 == true) id.AddClaim(new Claim("Right", $"{x.t.Code}:Suppression:{x.r.SubjectId}"));
            }
            return principal;
        }
    }
}
