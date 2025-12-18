using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aion.Domain.UI.Navigation;

/// <summary>
/// Provides a centralized navigation API for opening, focusing and closing Aion modules.
/// </summary>
public interface IAionNavigationService
{
    Task OpenModuleAsync(string moduleKey, object? parameters = null, CancellationToken ct = default);
    Task CloseModuleAsync(string moduleKey, CancellationToken ct = default);
    Task CloseAllAsync(CancellationToken ct = default);
    Task FocusModuleAsync(string moduleKey, CancellationToken ct = default);
    IReadOnlyList<OpenModuleTab> GetOpenTabs();
}
