using Aion.DataEngine.Entities;
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
