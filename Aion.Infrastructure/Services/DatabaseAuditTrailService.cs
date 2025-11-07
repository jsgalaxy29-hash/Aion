using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Abstractions;
using Aion.AI.Models;
using Aion.Domain.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aion.Infrastructure.Services;

public sealed class DatabaseAuditTrailService : IAuditTrailService
{
    private readonly IDbContextFactory<AionDbContext> _dbContextFactory;
    private readonly ILogger<DatabaseAuditTrailService> _logger;

    public DatabaseAuditTrailService(IDbContextFactory<AionDbContext> dbContextFactory, ILogger<DatabaseAuditTrailService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task RecordAsync(AuditRecord record, CancellationToken ct = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = new SXGenerationLog
        {
            RequestText = record.RequestText,
            IntentsJson = record.IntentsJson,
            PlanJson = record.PlanJson,
            PatchYaml = record.PatchYaml,
            ArtifactsSummary = record.ArtifactsSummary,
            Status = record.Status.ToString(),
            ErrorMessage = record.ErrorMessage,
            ModelVersion = record.ModelVersion
        };

        await dbContext.SXGenerationLogs.AddAsync(entity, ct).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogDebug("📘 Journal IA sauvegardé avec le statut {Status}", entity.Status);
    }
}
