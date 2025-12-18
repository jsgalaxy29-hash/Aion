using System.Threading;
using System.Threading.Tasks;

namespace Aion.DataEngine.Interfaces;

public interface IAionProvisioningObserver
{
    Task OnStructureCreatedAsync(CancellationToken cancellationToken = default);
}
