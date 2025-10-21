using System.Threading;
using System.Threading.Tasks;

namespace Aion.Domain.Contracts
{
    /// <summary>
    /// Service d'authentification. Gère la connexion et la déconnexion de l'utilisateur.
    /// </summary>
    public interface IAuthService
    {
        Task<SignInResult> SignInAsync(string login, string password, CancellationToken ct);
        Task SignOutAsync(CancellationToken ct);
    }

    /// <summary>
    /// Modèle pour le résultat d'un login. Contient un succès booléen et éventuellement un message.
    /// </summary>
    public sealed class SignInResult
    {
        public bool Succeeded { get; init; }
        public string? ErrorMessage { get; init; }

        public static readonly SignInResult Success = new() { Succeeded = true };

        public static SignInResult Fail(string message) => new() { Succeeded = false, ErrorMessage = message };
    }
}
