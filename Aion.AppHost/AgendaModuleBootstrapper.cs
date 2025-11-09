using Aion.Domain.Contracts;
using Aion.Domain.UI;

namespace Aion.AppHost;

/// <summary>
/// Publishes the agenda routes into the shared route registry so the tab host can resolve the component.
/// </summary>
public sealed class AgendaModuleBootstrapper : IModuleBootstrapper
{
    public void Register()
    {
        RouteRegistry.Register("/agenda", typeof(Pages.Agenda));
    }
}
