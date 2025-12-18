using Aion.Domain.Contracts;
using Aion.Domain.UI;

namespace Aion.Module.TableManager;

public sealed class TableManagerBootstrapper : IModuleBootstrapper
{
    public void Register()
    {
        RouteRegistry.Register("/dynamic/manager", typeof(Pages.TableManager));
    }
}
