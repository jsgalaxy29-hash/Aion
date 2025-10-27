using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Threading;
using System.Threading.Tasks;

namespace Aion.Domain.Contracts
{
    public interface IAuthService
    {
        // Connexion - renvoie un résultat de connexion
        Task<SignInResult> SignInAsync(string login, string password, CancellationToken ct = default);
        // Déconnexion
        Task SignOutAsync(CancellationToken ct = default);
        // Méthodes compatibles avec la page Login (booléen)
        Task<bool> LoginAsync(string login, string password, int tenantId, bool rememberMe, CancellationToken ct = default);
        Task LogoutAsync(CancellationToken ct = default);
    }
}
