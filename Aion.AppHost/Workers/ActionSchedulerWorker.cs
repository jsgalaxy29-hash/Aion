using System;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.Agenda;
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
        using var scope = _scopeFactory.CreateScope();
        var scheduledActionService = scope.ServiceProvider.GetRequiredService<IScheduledActionService>();

        var dueActions = await scheduledActionService.GetDueActionsAsync(DateTime.UtcNow, ct).ConfigureAwait(false);
        if (dueActions.Count == 0)
        {
            return;
        }

        foreach (var action in dueActions)
        {
            try
            {
                _logger.LogInformation("Exécution simulée de l'action planifiée {ActionId} - {Libelle}", action.Id, action.Libelle);
                await scheduledActionService.MarkExecutedAsync(action, DateTime.UtcNow, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'exécution de l'action planifiée {ActionId}", action.Id);
            }
        }
    }
}
