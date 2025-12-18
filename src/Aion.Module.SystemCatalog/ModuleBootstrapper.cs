using Aion.Domain.Contracts;
using Aion.Domain.UI;

namespace Aion.Module.SystemCatalog
{
    public sealed class SystemCatalogBootstrapper : IModuleBootstrapper
    {
        public void Register()
        {
            RouteRegistry.Register("/admin/catalog", typeof(Pages.CatalogDesigner));
        }
    }
}
