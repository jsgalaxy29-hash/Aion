using System.Threading;
using System.Threading.Tasks;

namespace Aion.Domain.UI.DynamicLayouts
{
    /// <summary>
    /// Abstraction pour persister les préférences de mise en page des grilles dynamiques.
    /// </summary>
    public interface IDynamicLayoutStore
    {
        Task<DynamicGridLayout?> LoadLayoutAsync(string tableName, int tenantId, int userId, CancellationToken ct);
        Task SaveLayoutAsync(string tableName, int tenantId, int userId, DynamicGridLayout layout, CancellationToken ct);
    }
}
