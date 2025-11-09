using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aion.Domain.Agenda;

public interface IScheduledActionService
{
    Task<List<SScheduledAction>> GetDueActionsAsync(DateTime nowUtc, CancellationToken ct = default);
    Task MarkExecutedAsync(SScheduledAction action, DateTime nowUtc, CancellationToken ct = default);
}
