using Aion.DataEngine.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Threading;
using System.Threading.Tasks;

namespace Aion.Domain.Contracts
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(string username, string password, int tenantId, bool rememberMe = false);
        Task LogoutAsync();
        Task<SUser?> GetCurrentUserAsync();
    }
}
