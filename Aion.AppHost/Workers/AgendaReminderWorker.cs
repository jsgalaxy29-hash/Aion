using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.Agenda;
using Aion.Infrastructure;
using Microsoft.AspNetCore.Http;
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
        List<int> tenantIds;
        using (var discoveryScope = _scopeFactory.CreateScope())
        {
            var discoveryFactory = discoveryScope.ServiceProvider.GetRequiredService<IDbContextFactory<AionDbContext>>();
            tenantIds = await TenantExecutionHelper.GetTenantIdsAsync(discoveryFactory, ct).ConfigureAwait(false);
        }

        if (tenantIds.Count == 0)
        {
            return;
        }

        var nowUtc = DateTime.UtcNow;

        foreach (var tenantId in tenantIds)
        {
            using var tenantScope = _scopeFactory.CreateScope();
            var dbFactory = tenantScope.ServiceProvider.GetRequiredService<IDbContextFactory<AionDbContext>>();
            var notificationService = tenantScope.ServiceProvider.GetRequiredService<INotificationService>();
            var httpContextAccessor = tenantScope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

            try
            {
                await ProcessTenantRemindersAsync(dbFactory, notificationService, httpContextAccessor, tenantId, nowUtc, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement des rappels pour le tenant {TenantId}", tenantId);
            }
        }
    }

    private async Task ProcessTenantRemindersAsync(
        IDbContextFactory<AionDbContext> dbFactory,
        INotificationService notificationService,
        IHttpContextAccessor httpContextAccessor,
        int tenantId,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var previousContext = httpContextAccessor.HttpContext;
        try
        {
            httpContextAccessor.HttpContext = TenantExecutionHelper.CreateBackgroundHttpContext(tenantId);

            await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

            var reminders = await db.SAgendaReminders
                .Include(r => r.AgendaEvent)
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
                    _logger.LogError(ex, "Erreur lors de l'envoi du rappel {ReminderId} pour le tenant {TenantId}", reminder.Id, tenantId);
                }
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            httpContextAccessor.HttpContext = previousContext;
        }
    }
}
