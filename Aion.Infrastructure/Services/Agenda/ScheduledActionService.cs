using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.Agenda;
using Aion.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Aion.Infrastructure.Services.Agenda;

public class ScheduledActionService(IDbContextFactory<AionDbContext> dbContextFactory) : IScheduledActionService
{
    private readonly IDbContextFactory<AionDbContext> _dbContextFactory = dbContextFactory;

    public async Task<List<SScheduledAction>> GetDueActionsAsync(DateTime nowUtc, CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        return await db.SScheduledActions
            .Include(a => a.Status)
            .Where(a => !a.Deleted && a.Status != null && a.Status.Code == "ACTIVE" && a.NextRunUtc <= nowUtc)
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task MarkExecutedAsync(SScheduledAction action, DateTime nowUtc, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        var tracked = await db.SScheduledActions.FirstOrDefaultAsync(a => a.Id == action.Id, ct).ConfigureAwait(false);
        if (tracked == null)
        {
            return;
        }

        tracked.LastRunUtc = nowUtc;
        tracked.NextRunUtc = GetNextOccurrence(nowUtc, tracked.CronExpression);
        tracked.LastError = null;
        tracked.DtModification = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    private static DateTime GetNextOccurrence(DateTime fromUtc, string cronExpression)
    {
        // Implémentation simplifiée : seule la minute est gérée (ex: "*/5 * * * *").
        // Les expressions non reconnues replanifient l'action pour +1 heure.
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            return fromUtc.AddHours(1);
        }

        var parts = cronExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return fromUtc.AddHours(1);
        }

        var minutePart = parts[0];
        if (minutePart.StartsWith("*/", StringComparison.Ordinal) && int.TryParse(minutePart[2..], out var stepMinutes) && stepMinutes > 0)
        {
            return fromUtc.AddMinutes(stepMinutes);
        }

        if (int.TryParse(minutePart, out var minute) && minute >= 0 && minute < 60)
        {
            var next = new DateTime(fromUtc.Year, fromUtc.Month, fromUtc.Day, fromUtc.Hour, minute, 0, DateTimeKind.Utc);
            if (next <= fromUtc)
            {
                next = next.AddHours(1);
            }

            return next;
        }

        return fromUtc.AddHours(1);
    }
}
