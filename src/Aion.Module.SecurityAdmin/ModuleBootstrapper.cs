using Aion.Domain.Contracts;
using Aion.Domain.UI;

namespace Aion.Module.SecurityAdmin;

public sealed class SecurityAdminBootstrapper : IModuleBootstrapper
{
    public void Register()
    {
        RouteRegistry.Register("/admin/rights", typeof(Pages.GroupRights));
    }
}
