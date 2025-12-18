using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aion.DataEngine.Interfaces;
using Aion.Domain.Agenda;
using Aion.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Aion.Infrastructure.Services.Agenda;

public class AgendaService : IAgendaService
{
    private readonly IDbContextFactory<AionDbContext> _dbContextFactory;
    private readonly IUserContext _userContext;

    public AgendaService(IDbContextFactory<AionDbContext> dbContextFactory, IUserContext userContext)
    {
        _dbContextFactory = dbContextFactory;
        _userContext = userContext;
    }

    public async Task<SAgendaEvent> CreateEventAsync(SAgendaEvent evt, IEnumerable<SAgendaReminder>? reminders, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(evt);

        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        await EnsureAgendaWriteAccessAsync(db, evt.AgendaId, ct).ConfigureAwait(false);

        evt.Id = 0;
        evt.TenantId = _userContext.TenantId;
        evt.DtCreation = DateTime.UtcNow;
        evt.DtModification = DateTime.UtcNow;
        evt.UsrCreationId = _userContext.UserId;
        evt.UsrModificationId = _userContext.UserId;
        db.SAgendaEvents.Add(evt);
        await db.SaveChangesAsync(ct);

        if (reminders != null)
        {
            foreach (var reminder in reminders)
            {
                reminder.Id = 0;
                reminder.AgendaEventId = evt.Id;
                reminder.TenantId = _userContext.TenantId;
                reminder.DtCreation = DateTime.UtcNow;
                reminder.DtModification = DateTime.UtcNow;
                reminder.UsrCreationId = _userContext.UserId;
                reminder.UsrModificationId = _userContext.UserId;
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
            .FirstOrDefaultAsync(e => e.Id == evt.Id && e.TenantId == _userContext.TenantId, ct)
            .ConfigureAwait(false);

        if (existing == null)
        {
            throw new InvalidOperationException($"Événement {evt.Id} introuvable");
        }

        await EnsureAgendaWriteAccessAsync(db, existing.AgendaId, ct).ConfigureAwait(false);

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
        existing.UsrModificationId = _userContext.UserId;

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
                reminder.UsrCreationId = _userContext.UserId;
                reminder.UsrModificationId = _userContext.UserId;
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
            .FirstOrDefaultAsync(e => e.Id == eventId && e.TenantId == _userContext.TenantId, ct)
            .ConfigureAwait(false);

        if (existing == null)
        {
            return;
        }

        await EnsureAgendaWriteAccessAsync(db, existing.AgendaId, ct).ConfigureAwait(false);

        db.SAgendaReminders.RemoveRange(existing.Reminders);
        db.SAgendaEvents.Remove(existing);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<SAgendaEvent>> GetEventsForUserAsync(int userId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        if (userId != _userContext.UserId)
        {
            throw new UnauthorizedAccessException("Impossible de consulter l'agenda d'un autre utilisateur.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        var ownedAgendaIds = await db.SAgendas
            .Where(a => a.OwnerUserId == userId && a.TenantId == _userContext.TenantId && !a.Deleted)
            .Select(a => a.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var sharedEntries = await db.SAgendaUsers
            .Where(a => a.UserId == userId && a.TenantId == _userContext.TenantId && !a.Deleted)
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
                && e.TenantId == _userContext.TenantId
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
    private async Task EnsureAgendaWriteAccessAsync(AionDbContext db, int agendaId, CancellationToken ct)
    {
        var agenda = await db.SAgendas
            .AsNoTracking()
            .Where(a => a.Id == agendaId && a.TenantId == _userContext.TenantId && !a.Deleted)
            .Select(a => new { a.OwnerUserId })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (agenda == null)
        {
            throw new InvalidOperationException($"Agenda {agendaId} introuvable pour le locataire {_userContext.TenantId}.");
        }

        if (agenda.OwnerUserId == _userContext.UserId)
        {
            return;
        }

        var canEdit = await db.SAgendaUsers
            .AsNoTracking()
            .Where(s => s.AgendaId == agendaId
                && s.UserId == _userContext.UserId
                && s.TenantId == _userContext.TenantId
                && !s.Deleted)
            .Select(s => (bool?)s.CanEdit)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (canEdit is not true)
        {
            throw new UnauthorizedAccessException("Droits insuffisants pour modifier cet agenda.");
        }
    }
}
