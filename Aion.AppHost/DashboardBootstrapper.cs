using Aion.Domain.Contracts;
using Aion.Domain.UI;
using Aion.AppHost.Pages;

namespace Aion.AppHost;

public sealed class DashboardBootstrapper : IModuleBootstrapper
{
    public void Register()
    {
        RouteRegistry.Register("/", typeof(Dashboard));
        RouteRegistry.Register("/dashboard", typeof(Dashboard));
    }
}
