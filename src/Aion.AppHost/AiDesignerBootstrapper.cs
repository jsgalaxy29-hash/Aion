using Aion.Domain.Contracts;
using Aion.Domain.UI;

namespace Aion.AppHost;

/// <summary>
/// Publishes the AI Designer route so it can be opened from the module catalog.
/// </summary>
public sealed class AiDesignerBootstrapper : IModuleBootstrapper
{
    public void Register()
    {
        RouteRegistry.Register("/modules/ai-designer", typeof(Pages.AiDesigner));
    }
}
