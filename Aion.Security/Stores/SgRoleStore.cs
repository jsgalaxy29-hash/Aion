using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Aion.DataEngine.Entities;

namespace Aion.Security.Stores
{
    public class SgRoleStore : IRoleStore<IdentityRole<Guid>>
    {
        private readonly SecurityDbContext _db;
        public SgRoleStore(SecurityDbContext db) => _db = db;

        public async Task<IdentityResult> CreateAsync(IdentityRole<Guid> role, CancellationToken cancellationToken)
        {
            _db.SGroup.Add(new SGroup { Name = role.Name ?? $"G-{role.Id}" });
            await _db.SaveChangesAsync(cancellationToken);
            return IdentityResult.Success;
        }
        public async Task<IdentityResult> UpdateAsync(IdentityRole<Guid> role, CancellationToken cancellationToken)
        {
            var g = await _db.SGroup.FirstOrDefaultAsync(x => x.Name == role.Name, cancellationToken);
            if (g is null) return IdentityResult.Failed(new IdentityError { Description = "Group not found" });
            g.Name = role.Name ?? g.Name;
            await _db.SaveChangesAsync(cancellationToken);
            return IdentityResult.Success;
        }
        public async Task<IdentityResult> DeleteAsync(IdentityRole<Guid> role, CancellationToken cancellationToken)
        {
            var g = await _db.SGroup.FirstOrDefaultAsync(x => x.Name == role.Name, cancellationToken);
            if (g is null) return IdentityResult.Success;
            _db.SGroup.Remove(g);
            await _db.SaveChangesAsync(cancellationToken);
            return IdentityResult.Success;
        }
        public Task<string> GetRoleIdAsync(IdentityRole<Guid> role, CancellationToken cancellationToken) => Task.FromResult(role.Id.ToString());
        public Task<string?> GetRoleNameAsync(IdentityRole<Guid> role, CancellationToken cancellationToken) => Task.FromResult(role.Name);
        public Task SetRoleNameAsync(IdentityRole<Guid> role, string? roleName, CancellationToken cancellationToken) { role.Name = roleName; return Task.CompletedTask; }
        public Task<string?> GetNormalizedRoleNameAsync(IdentityRole<Guid> role, CancellationToken cancellationToken) => Task.FromResult(role.NormalizedName);
        public Task SetNormalizedRoleNameAsync(IdentityRole<Guid> role, string? normalizedName, CancellationToken cancellationToken) { role.NormalizedName = normalizedName; return Task.CompletedTask; }
        public Task<IdentityRole<Guid>?> FindByIdAsync(string roleId, CancellationToken cancellationToken) => Task.FromResult<IdentityRole<Guid>?>(null);
        public Task<IdentityRole<Guid>?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken) => Task.FromResult<IdentityRole<Guid>?>(new IdentityRole<Guid>(normalizedRoleName){ NormalizedName = normalizedRoleName });
        public void Dispose() {}
    }
}
