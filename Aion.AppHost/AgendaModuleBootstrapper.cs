using Aion.Domain.Contracts;
using Aion.Domain.UI;
using Aion.UI.Components.Agenda;

namespace Aion.AppHost;

/// <summary>
/// Publishes the agenda routes into the shared route registry so the tab host can resolve the component.
/// </summary>
public sealed class AgendaModuleBootstrapper : IModuleBootstrapper
{
    public void Register()
    {
        // La page Agenda est désormais fournie par la bibliothèque partagée Aion.UI.
        RouteRegistry.Register("/agenda", typeof(Agenda));
    }
}
