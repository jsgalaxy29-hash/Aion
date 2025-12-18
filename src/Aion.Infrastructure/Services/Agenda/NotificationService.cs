using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.Agenda;
using Aion.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Aion.Infrastructure.Services.Agenda;

public class NotificationService(IDbContextFactory<AionDbContext> dbContextFactory) : INotificationService
{
    private readonly IDbContextFactory<AionDbContext> _dbContextFactory = dbContextFactory;

    public async Task NotifyAsync(SNotification notification, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        notification.DtCreation = DateTime.UtcNow;
        notification.DtModification = DateTime.UtcNow;
        db.SNotifications.Add(notification);
        await db.SaveChangesAsync(ct);
    }

    public async Task SendReminderAsync(SAgendaReminder reminder, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(reminder);

        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        var evt = await db.SAgendaEvents
            .Include(e => e.Agenda)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == reminder.AgendaEventId && e.TenantId == reminder.TenantId, ct)
            .ConfigureAwait(false);

        if (evt?.Agenda == null)
        {
            return;
        }

        var notificationTypeId = await db.RNotificationTypes
            .AsNoTracking()
            .Where(t => t.Code == "AGENDA_REMINDER" && (t.TenantId == reminder.TenantId || t.TenantId == 0))
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var notification = new SNotification
        {
            UserId = evt.Agenda.OwnerUserId,
            Title = evt.Libelle,
            Message = $"Rappel pour l'événement '{evt.Libelle}'",
            CreatedUtc = DateTime.UtcNow,
            NotificationTypeId = notificationTypeId,
            LinkUrl = "/agenda",
            TenantId = evt.TenantId,
            Actif = true,
            Doc = false,
            Deleted = false,
            DtCreation = DateTime.UtcNow,
            DtModification = DateTime.UtcNow
        };

        db.SNotifications.Add(notification);
        await db.SaveChangesAsync(ct);
    }
}
