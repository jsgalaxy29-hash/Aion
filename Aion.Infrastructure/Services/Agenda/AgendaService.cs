using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.Agenda;
using Aion.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Aion.Infrastructure.Services.Agenda;

public class AgendaService(IDbContextFactory<AionDbContext> dbContextFactory) : IAgendaService
{
    private readonly IDbContextFactory<AionDbContext> _dbContextFactory = dbContextFactory;

    public async Task<SAgendaEvent> CreateEventAsync(SAgendaEvent evt, IEnumerable<SAgendaReminder>? reminders, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(evt);

        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        evt.Id = 0;
        evt.DtCreation = DateTime.UtcNow;
        evt.DtModification = DateTime.UtcNow;
        db.SAgendaEvents.Add(evt);
        await db.SaveChangesAsync(ct);

        if (reminders != null)
        {
            foreach (var reminder in reminders)
            {
                reminder.Id = 0;
                reminder.AgendaEventId = evt.Id;
                reminder.TenantId = evt.TenantId;
                reminder.DtCreation = DateTime.UtcNow;
                reminder.DtModification = DateTime.UtcNow;
                db.SAgendaReminders.Add(reminder);
            }

            await db.SaveChangesAsync(ct);
        }

        return evt;
    }

    public async Task UpdateEventAsync(SAgendaEvent evt, IEnumerable<SAgendaReminder>? reminders, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(evt);

        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        var existing = await db.SAgendaEvents
            .Include(e => e.Reminders)
            .FirstOrDefaultAsync(e => e.Id == evt.Id, ct)
            .ConfigureAwait(false);

        if (existing == null)
        {
            throw new InvalidOperationException($"Événement {evt.Id} introuvable");
        }

        existing.Libelle = evt.Libelle;
        existing.Description = evt.Description;
        existing.StartUtc = evt.StartUtc;
        existing.EndUtc = evt.EndUtc;
        existing.AllDay = evt.AllDay;
        existing.IsPrivate = evt.IsPrivate;
        existing.StatusId = evt.StatusId;
        existing.ContextEntityType = evt.ContextEntityType;
        existing.ContextEntityId = evt.ContextEntityId;
        existing.EnableReminders = evt.EnableReminders;
        existing.DtModification = DateTime.UtcNow;

        var currentReminders = existing.Reminders.ToList();
        if (currentReminders.Count > 0)
        {
            db.SAgendaReminders.RemoveRange(currentReminders);
        }

        if (reminders != null)
        {
            foreach (var reminder in reminders)
            {
                reminder.Id = 0;
                reminder.AgendaEventId = existing.Id;
                reminder.TenantId = existing.TenantId;
                reminder.DtCreation = DateTime.UtcNow;
                reminder.DtModification = DateTime.UtcNow;
                db.SAgendaReminders.Add(reminder);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteEventAsync(int eventId, CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        var existing = await db.SAgendaEvents
            .Include(e => e.Reminders)
            .FirstOrDefaultAsync(e => e.Id == eventId, ct)
            .ConfigureAwait(false);

        if (existing == null)
        {
            return;
        }

        db.SAgendaReminders.RemoveRange(existing.Reminders);
        db.SAgendaEvents.Remove(existing);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<SAgendaEvent>> GetEventsForUserAsync(int userId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        var ownedAgendaIds = await db.SAgendas
            .Where(a => a.OwnerUserId == userId && !a.Deleted)
            .Select(a => a.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var sharedEntries = await db.SAgendaUsers
            .Where(a => a.UserId == userId && !a.Deleted)
            .Select(a => new { a.AgendaId, a.CanViewPrivate })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var accessibleAgendaIds = ownedAgendaIds
            .Concat(sharedEntries.Select(s => s.AgendaId))
            .Distinct()
            .ToArray();

        if (accessibleAgendaIds.Length == 0)
        {
            return new List<SAgendaEvent>();
        }

        var sharedLookup = sharedEntries.ToDictionary(x => x.AgendaId, x => x.CanViewPrivate);
        var ownedSet = ownedAgendaIds.ToHashSet();

        var events = await db.SAgendaEvents
            .Include(e => e.Reminders)
            .Where(e => accessibleAgendaIds.Contains(e.AgendaId)
                && !e.Deleted
                && e.Actif
                && e.StartUtc < toUtc
                && e.EndUtc >= fromUtc)
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return events
            .Where(e => !e.IsPrivate
                || ownedSet.Contains(e.AgendaId)
                || (sharedLookup.TryGetValue(e.AgendaId, out var canView) && canView))
            .ToList();
    }
}
