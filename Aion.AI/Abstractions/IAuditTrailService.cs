using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Models;

namespace Aion.AI.Abstractions;

/// <summary>
/// Persists the generation trace for observability and troubleshooting.
/// </summary>
public interface IAuditTrailService
{
    Task RecordAsync(AuditRecord record, CancellationToken ct = default);
}
