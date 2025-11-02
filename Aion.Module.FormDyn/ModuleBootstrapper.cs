using Aion.Domain.Contracts;
using Aion.Domain.UI;

namespace Aion.Module.FormDyn
{
    public sealed class FormDynBootstrapper : IModuleBootstrapper
    {
        public void Register()
        {
            RouteRegistry.Register("/dynamic/form", typeof(Pages.DynamicForm));
        }
    }
}
