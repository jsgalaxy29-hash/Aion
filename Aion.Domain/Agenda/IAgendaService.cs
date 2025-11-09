using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aion.Domain.Agenda;

public interface IAgendaService
{
    Task<SAgendaEvent> CreateEventAsync(SAgendaEvent evt, IEnumerable<SAgendaReminder>? reminders, CancellationToken ct = default);
    Task UpdateEventAsync(SAgendaEvent evt, IEnumerable<SAgendaReminder>? reminders, CancellationToken ct = default);
    Task DeleteEventAsync(int eventId, CancellationToken ct = default);
    Task<List<SAgendaEvent>> GetEventsForUserAsync(int userId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
}
