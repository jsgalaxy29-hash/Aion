using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.Agenda;
using Aion.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aion.AppHost.Workers;

public class AgendaReminderWorker : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);
    private readonly ILogger<AgendaReminderWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public AgendaReminderWorker(ILogger<AgendaReminderWorker> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AgendaReminderWorker démarré");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation during delay/processing
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement des rappels d'agenda");
            }

            try
            {
                await Task.Delay(PollingInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("AgendaReminderWorker arrêté");
    }

    private async Task ProcessRemindersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AionDbContext>>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var nowUtc = DateTime.UtcNow;

        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var reminders = await db.SAgendaReminders
            .IgnoreQueryFilters()
            .Include(r => r.AgendaEvent)
                .ThenInclude(e => e.Agenda)
            .Where(r => !r.Deleted && !r.IsSent && r.TriggerAtUtc <= nowUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (reminders.Count == 0)
        {
            return;
        }

        foreach (var reminder in reminders)
        {
            try
            {
                await notificationService.SendReminderAsync(reminder, ct).ConfigureAwait(false);
                reminder.IsSent = true;
                reminder.SentAtUtc = nowUtc;
                reminder.DtModification = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi du rappel {ReminderId}", reminder.Id);
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
