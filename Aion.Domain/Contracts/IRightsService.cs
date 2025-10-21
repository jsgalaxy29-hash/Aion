using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aion.Domain.Contracts
{
    /// <summary>
    /// Service d'autorisation. Permet de vérifier si un utilisateur possède un droit donné.
    /// </summary>
    public interface IRightsService
    {
        /// <summary>
        /// Détermine si un menu est autorisé pour l'utilisateur.
        /// </summary>
        Task<bool> IsMenuAuthorizedAsync(Guid tenantId, Guid userId, int menuId, CancellationToken ct);

        /// <summary>
        /// Récupère tous les identifiants de menus autorisés pour l'utilisateur.
        /// </summary>
        Task<HashSet<int>> GetAuthorizedMenuIdsAsync(Guid tenantId, Guid userId, CancellationToken ct);
    }
}
