using Aion.Security.Models;

namespace Aion.Security.Services
{
    /// <summary>
    /// Service de gestion et vérification des droits utilisateurs.
    /// Implémente la logique RBAC (Role-Based Access Control) d'Aion.
    /// </summary>
    public interface IRightService
    {
        /// <summary>
        /// Vérifie si un utilisateur possède un droit spécifique sur une ressource.
        /// Applique la règle de fusion : true > false (le droit le plus permissif gagne).
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur</param>
        /// <param name="tenantId">Identifiant du tenant</param>
        /// <param name="target">Type de ressource (Menu, Module, Table, Action...)</param>
        /// <param name="subjectId">Identifiant de la ressource</param>
        /// <param name="flag">Droit à vérifier (Right1-5)</param>
        /// <param name="ct">Token d'annulation</param>
        Task<bool> HasRightAsync(int userId, int tenantId, string target, int subjectId, RightFlag flag, CancellationToken ct = default);

        /// <summary>
        /// Récupère tous les droits d'un utilisateur (agrégation de ses groupes).
        /// Résultat mis en cache pour optimisation.
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur</param>
        /// <param name="tenantId">Identifiant du tenant</param>
        /// <param name="ct">Token d'annulation</param>
        Task<Dictionary<string, List<UserRights>>> GetUserRightsAsync(int userId, int tenantId, CancellationToken ct = default);

        /// <summary>
        /// Récupère les identifiants de menus autorisés pour un utilisateur.
        /// Utilisé pour filtrer la navigation.
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur</param>
        /// <param name="tenantId">Identifiant du tenant</param>
        /// <param name="ct">Token d'annulation</param>
        Task<List<int>> GetAuthorizedMenuIdsAsync(int userId, int tenantId, CancellationToken ct = default);

        /// <summary>
        /// Invalide le cache des droits pour un utilisateur.
        /// À appeler après modification des groupes ou droits.
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur</param>
        void InvalidateCache(int userId);
    }
}