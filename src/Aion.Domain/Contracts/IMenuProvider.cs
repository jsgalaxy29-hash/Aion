using Aion.DataEngine.Entities;

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
