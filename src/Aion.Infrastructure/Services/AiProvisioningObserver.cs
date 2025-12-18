using System.Threading;
using System.Threading.Tasks;
using Aion.DataEngine.Interfaces;
using Aion.Infrastructure.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aion.Infrastructure.Services;

public sealed class AiProvisioningObserver : IAionProvisioningObserver
{
    private readonly IDbContextFactory<AionDbContext> _dbContextFactory;
    private readonly ILogger<AiProvisioningObserver> _logger;

    public AiProvisioningObserver(IDbContextFactory<AionDbContext> dbContextFactory, ILogger<AiProvisioningObserver> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task OnStructureCreatedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🧠 Application des migrations EF Core pour le module IA...");
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("✅ Migrations EF Core appliquées");

        _logger.LogInformation("🪴 Seed des données système IA...");
        await AiSystemSeeder.SeedAsync(dbContext, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("✅ Données IA initialisées");
    }
}
