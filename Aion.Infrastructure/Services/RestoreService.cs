using System.Data.Common;
using Aion.DataEngine.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aion.Infrastructure.Services;

public sealed class RestoreService : IRestoreService
{
    private readonly IDbContextFactory<AionDbContext> _dbContextFactory;
    private readonly ILogger<RestoreService> _logger;

    public RestoreService(IDbContextFactory<AionDbContext> dbContextFactory, ILogger<RestoreService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task RestoreAsync(string backupPath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(backupPath))
        {
            throw new ArgumentException("Backup path must be provided", nameof(backupPath));
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var database = dbContext.Database;
        var providerName = database.ProviderName ?? "unknown";
        var connection = database.GetDbConnection();

        if (providerName.Contains("sqlserver", StringComparison.OrdinalIgnoreCase))
        {
            await RestoreSqlServerAsync(connection, backupPath, ct).ConfigureAwait(false);
        }
        else if (providerName.Contains("sqlite", StringComparison.OrdinalIgnoreCase))
        {
            await RestoreSqliteAsync(connection, backupPath, ct).ConfigureAwait(false);
        }
        else
        {
            throw new NotSupportedException($"Restore provider not supported: {providerName}");
        }

        _logger.LogInformation("♻️ Base restaurée depuis {Backup}", backupPath);
    }

    private static async Task RestoreSqlServerAsync(DbConnection connection, string backupPath, CancellationToken ct)
    {
        if (connection is not SqlConnection sqlConnection)
        {
            throw new InvalidOperationException("SqlServer restore requires a SqlConnection");
        }

        await sqlConnection.OpenAsync(ct).ConfigureAwait(false);

        var restoreSql = $@"ALTER DATABASE [{sqlConnection.Database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [{sqlConnection.Database}] FROM DISK = @path WITH REPLACE;
ALTER DATABASE [{sqlConnection.Database}] SET MULTI_USER;";

        await using var command = sqlConnection.CreateCommand();
        command.CommandText = restoreSql;
        command.Parameters.Add(new SqlParameter("@path", backupPath));
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task RestoreSqliteAsync(DbConnection connection, string backupPath, CancellationToken ct)
    {
        var builder = new SqliteConnectionStringBuilder(connection.ConnectionString);
        var destinationPath = builder.DataSource;

        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new InvalidOperationException("Cannot determine Sqlite destination path from connection string.");
        }

        var destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        await using (var source = new SqliteConnection($"Data Source={backupPath}"))
        await using (var destination = new SqliteConnection(connection.ConnectionString))
        {
            await source.OpenAsync(ct).ConfigureAwait(false);
            await destination.OpenAsync(ct).ConfigureAwait(false);
            source.BackupDatabase(destination);
        }
    }
}
