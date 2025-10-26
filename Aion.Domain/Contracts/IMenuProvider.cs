using Aion.DataEngine.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aion.Domain.Contracts
{
    /// <summary>
    /// Service de récupération des menus avec filtrage par droits.
    /// </summary>
    public interface IMenuProvider
    {
        Task<IReadOnlyList<SMenu>> GetAuthorizedMenuAsync(int tenantId, int userId, CancellationToken ct);
        Task<IReadOnlyList<SMenu>> GetAllMenuAsync(int tenantId, CancellationToken ct);
    }
}
