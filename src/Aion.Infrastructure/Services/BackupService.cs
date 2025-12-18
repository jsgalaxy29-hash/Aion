using System.Data.Common;
using Aion.DataEngine.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aion.Infrastructure.Services;

public sealed class BackupService : IBackupService
{
    private readonly IDbContextFactory<AionDbContext> _dbContextFactory;
    private readonly ILogger<BackupService> _logger;

    public BackupService(IDbContextFactory<AionDbContext> dbContextFactory, ILogger<BackupService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<BackupResult> BackupAsync(string? destinationFolder = null, CancellationToken ct = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var database = dbContext.Database;
        var providerName = database.ProviderName ?? "unknown";
        var connection = database.GetDbConnection();
        var dbName = connection.Database;

        var backupsFolder = destinationFolder ?? Path.Combine(AppContext.BaseDirectory, "backups");
        Directory.CreateDirectory(backupsFolder);

        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var extension = providerName.Contains("sqlite", StringComparison.OrdinalIgnoreCase) ? "db" : "bak";
        var backupPath = Path.Combine(backupsFolder, $"{dbName}-{timestamp}.{extension}");

        if (providerName.Contains("sqlserver", StringComparison.OrdinalIgnoreCase))
        {
            await BackupSqlServerAsync(connection, backupPath, ct).ConfigureAwait(false);
        }
        else if (providerName.Contains("sqlite", StringComparison.OrdinalIgnoreCase))
        {
            await BackupSqliteAsync(connection, backupPath, ct).ConfigureAwait(false);
        }
        else
        {
            throw new NotSupportedException($"Backup provider not supported: {providerName}");
        }

        var result = new BackupResult(providerName, dbName, backupPath, DateTimeOffset.UtcNow);
        _logger.LogInformation("💾 Sauvegarde terminée : {BackupPath}", backupPath);
        return result;
    }

    private static async Task BackupSqlServerAsync(DbConnection connection, string backupPath, CancellationToken ct)
    {
        if (connection is not SqlConnection sqlConnection)
        {
            throw new InvalidOperationException("SqlServer backup requires a SqlConnection");
        }

        await sqlConnection.OpenAsync(ct).ConfigureAwait(false);
        await using var command = sqlConnection.CreateCommand();
        command.CommandText = $"BACKUP DATABASE [{sqlConnection.Database}] TO DISK = @path WITH FORMAT, INIT, SKIP";
        command.Parameters.Add(new SqlParameter("@path", backupPath));
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task BackupSqliteAsync(DbConnection connection, string backupPath, CancellationToken ct)
    {
        if (connection is not SqliteConnection sqliteConnection)
        {
            sqliteConnection = new SqliteConnection(connection.ConnectionString);
        }

        await sqliteConnection.OpenAsync(ct).ConfigureAwait(false);
        await using var destination = new SqliteConnection($"Data Source={backupPath}");
        await destination.OpenAsync(ct).ConfigureAwait(false);
        sqliteConnection.BackupDatabase(destination);
    }
}
