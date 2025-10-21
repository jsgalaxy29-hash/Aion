using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.UI;

namespace Aion.Domain.Contracts
{
    /// <summary>
    /// Fournit la hiérarchie des menus autorisés pour un utilisateur.
    /// </summary>
    public interface IMenuProvider
    {
        /// <summary>
        /// Récupère la liste des menus visibles pour un utilisateur dans un tenant donné.
        /// Les conteneurs sans feuilles autorisées sont exclus.
        /// </summary>
        /// <param name="tenantId">Identifiant du tenant.</param>
        /// <param name="userId">Identifiant de l'utilisateur.</param>
        /// <param name="ct">Token d'annulation.</param>
        Task<IReadOnlyList<MenuEntity>> GetAuthorizedMenuAsync(Guid tenantId, Guid userId, CancellationToken ct);
    }
}
