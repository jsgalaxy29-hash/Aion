using Aion.Domain.Contracts;
using Aion.Domain.UI;

namespace Aion.Module.ListDyn
{
    public sealed class ListDynBootstrapper : IModuleBootstrapper
    {
        public void Register()
        {
            RouteRegistry.Register("/dynamic/list", typeof(Pages.DynamicList));
        }
    }
}
