using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Aion.Security.Entities;
using Aion.DataEngine.Entities;

namespace Aion.Security.Stores
{
    public class SgUserStore : IUserStore<SUser>
    {
        private readonly SecurityDbContext _db;
        public SgUserStore(SecurityDbContext db) => _db = db;

        public Task<string> GetUserIdAsync(SUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id.ToString());
        public Task<string?> GetUserNameAsync(SUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);
        public Task SetUserNameAsync(SUser user, string? userName, CancellationToken cancellationToken) { user.UserName = userName; return Task.CompletedTask; }
        public Task<string?> GetNormalizedUserNameAsync(SUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);
        public Task SetNormalizedUserNameAsync(SUser user, string? normalizedName, CancellationToken cancellationToken) { user.NormalizedUserName = normalizedName; return Task.CompletedTask; }

        public async Task<IdentityResult> CreateAsync(SUser user, CancellationToken cancellationToken) { _db.Users.Add(user); await _db.SaveChangesAsync(cancellationToken); return IdentityResult.Success; }
        public async Task<IdentityResult> UpdateAsync(SUser user, CancellationToken cancellationToken) { _db.Users.Update(user); await _db.SaveChangesAsync(cancellationToken); return IdentityResult.Success; }
        public async Task<IdentityResult> DeleteAsync(SUser user, CancellationToken cancellationToken) { _db.Users.Remove(user); await _db.SaveChangesAsync(cancellationToken); return IdentityResult.Success; }
        public Task<SUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) { if (!Guid.TryParse(userId, out var id)) return Task.FromResult<SUser?>(null); return _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)!; }
        public Task<SUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => _db.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken)!;
        public void Dispose() {}
    }
}
