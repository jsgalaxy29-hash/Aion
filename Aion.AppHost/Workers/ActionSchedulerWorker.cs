using System;
using System.Collections.Generic;
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

public class ActionSchedulerWorker : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);
    private readonly ILogger<ActionSchedulerWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public ActionSchedulerWorker(ILogger<ActionSchedulerWorker> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ActionSchedulerWorker démarré");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur dans ActionSchedulerWorker");
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

        _logger.LogInformation("ActionSchedulerWorker arrêté");
    }

    private async Task DispatchAsync(CancellationToken ct)
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
            var scheduledActionService = tenantScope.ServiceProvider.GetRequiredService<IScheduledActionService>();
            var httpContextAccessor = tenantScope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

            try
            {
                await DispatchForTenantAsync(scheduledActionService, httpContextAccessor, tenantId, nowUtc, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'exécution des actions planifiées pour le tenant {TenantId}", tenantId);
            }
        }
    }

    private async Task DispatchForTenantAsync(
        IScheduledActionService scheduledActionService,
        IHttpContextAccessor httpContextAccessor,
        int tenantId,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var previousContext = httpContextAccessor.HttpContext;
        try
        {
            httpContextAccessor.HttpContext = TenantExecutionHelper.CreateBackgroundHttpContext(tenantId);

            var dueActions = await scheduledActionService.GetDueActionsAsync(nowUtc, ct).ConfigureAwait(false);
            if (dueActions.Count == 0)
            {
                return;
            }

            foreach (var action in dueActions)
            {
                try
                {
                    _logger.LogInformation("Exécution simulée de l'action planifiée {ActionId} - {Libelle} (tenant {TenantId})", action.Id, action.Libelle, tenantId);
                    await scheduledActionService.MarkExecutedAsync(action, DateTime.UtcNow, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de l'exécution de l'action planifiée {ActionId} pour le tenant {TenantId}", action.Id, tenantId);
                }
            }
        }
        finally
        {
            httpContextAccessor.HttpContext = previousContext;
        }
    }
}
