using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Aion.Infrastructure.Data
{
    /// <summary>
    /// Implémentation de IDataProvider pour SQLite.
    /// </summary>
    public class SqliteDataProvider : DataEngine.Interfaces.IDataProvider
    {
        private string _connectionString;
        private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
        private bool _databaseInitialized;

        public SqliteDataProvider(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AionDb")
                ?? throw new InvalidOperationException("Connection string 'AionDb' not found");

            var builder = new SqliteConnectionStringBuilder(_connectionString);
            var dataSource = builder.DataSource;

            if (!string.IsNullOrWhiteSpace(dataSource)
                && !string.Equals(dataSource, ":memory:", StringComparison.OrdinalIgnoreCase)
                && !dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                var databasePath = dataSource;
                if (!Path.IsPathRooted(databasePath))
                {
                    databasePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, databasePath));
                }

                databasePath = Path.GetFullPath(databasePath);
                var directory = Path.GetDirectoryName(databasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                builder.DataSource = databasePath;
                _connectionString = builder.ConnectionString;
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, IDictionary<string, object?>? parameters = null)
        {
            await EnsureDatabaseExistsAsync().ConfigureAwait(false);

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            using var command = new SqliteCommand(sql, connection)
            {
                CommandTimeout = 120
            };

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            var result = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            return result;
        }

        public async Task<object?> ExecuteScalarAsync(string sql, IDictionary<string, object?>? parameters = null)
        {
            await EnsureDatabaseExistsAsync().ConfigureAwait(false);

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            using var command = new SqliteCommand(sql, connection)
            {
                CommandTimeout = 120
            };

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
            return result == DBNull.Value ? null : result;
        }

        public async Task<DataTable> ExecuteQueryAsync(string sql, IDictionary<string, object?>? parameters = null)
        {
            await EnsureDatabaseExistsAsync().ConfigureAwait(false);

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            using var command = new SqliteCommand(sql, connection)
            {
                CommandTimeout = 120
            };

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            var dataTable = new DataTable();
            dataTable.Load(reader);

            return dataTable;
        }

        private async Task EnsureDatabaseExistsAsync()
        {
            if (_databaseInitialized)
            {
                return;
            }

            await _initializationSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_databaseInitialized)
                {
                    return;
                }

                var builder = new SqliteConnectionStringBuilder(_connectionString);
                var dataSource = builder.DataSource;

                if (!string.IsNullOrWhiteSpace(dataSource)
                    && !string.Equals(dataSource, ":memory:", StringComparison.OrdinalIgnoreCase)
                    && !dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                {
                    if (!File.Exists(dataSource))
                    {
                        await using var connection = new SqliteConnection(_connectionString);
                        await connection.OpenAsync().ConfigureAwait(false);
                    }
                }
                else
                {
                    await using var connection = new SqliteConnection(_connectionString);
                    await connection.OpenAsync().ConfigureAwait(false);
                }

                _databaseInitialized = true;
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }
    }
}

