using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Abstractions;
using Aion.AI.Models;

namespace Aion.AI.Services;

/// <summary>
/// No-op audit trail used by default, host applications can override with a persistent implementation.
/// </summary>
public sealed class NullAuditTrailService : IAuditTrailService
{
    public Task RecordAsync(AuditRecord record, CancellationToken ct = default) => Task.CompletedTask;
}
