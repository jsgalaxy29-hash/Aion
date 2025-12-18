using Aion.Domain.UI;
using Aion.Domain.Contracts;

namespace Aion.Module.CRM
{
    /// <summary>
    /// Enregistre les routes et ressources propres au module CRM.
    /// </summary>
    public sealed class CrmBootstrapper : IModuleBootstrapper
    {
        public void Register()
        {
            // Enregistrer la route vers le composant OppsList
            RouteRegistry.Register("/modules/crm/opps", typeof(Pages.OppsList));
        }
    }
}
