using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.UI.State;

namespace Aion.Domain.Contracts
{
    /// <summary>
    /// Service permettant de lire et de sauvegarder la disposition du tableau de bord utilisateur.
    /// </summary>
    public interface IUserDashboardService
    {
        /// <summary>
        /// Récupère la disposition actuelle des widgets pour un utilisateur.
        /// </summary>
        Task<IReadOnlyList<UserDashboardLayoutEntity>> GetLayoutAsync(int tenantId, int userId, CancellationToken ct);

        /// <summary>
        /// Sauvegarde la disposition de widgets.
        /// </summary>
        Task SaveLayoutAsync(int tenantId, int userId, IEnumerable<UserDashboardLayoutEntity> layout, CancellationToken ct);
    }
}
