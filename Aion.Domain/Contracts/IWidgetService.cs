using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.Widgets;

namespace Aion.Domain.Contracts
{
    /// <summary>
    /// Service permettant de récupérer le catalogue de widgets et d'obtenir les données d'un widget.
    /// </summary>
    public interface IWidgetService
    {
        Task<IReadOnlyList<WidgetEntity>> GetAvailableWidgetsAsync(Guid tenantId, CancellationToken ct);
        Task<object?> GetDataAsync(string widgetCode, IDictionary<string, object?>? settings, CancellationToken ct);
    }
}
