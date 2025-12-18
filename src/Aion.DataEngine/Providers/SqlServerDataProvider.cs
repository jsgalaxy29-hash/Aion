using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Aion.Infrastructure.Data
{
    /// <summary>
    /// Implémentation de IDataProvider pour SQL Server.
    /// </summary>
    public class SqlDataProvider : DataEngine.Interfaces.IDataProvider
    {
        private readonly string _connectionString;
        private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
        private bool _databaseInitialized;

        public SqlDataProvider(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AionDb")
                ?? throw new InvalidOperationException("Connection string 'AionDb' not found");
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, IDictionary<string, object?>? parameters = null)
        {
            await EnsureDatabaseExistsAsync().ConfigureAwait(false);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 120; // 2 minutes pour les scripts complexes

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            int result = await command.ExecuteNonQueryAsync().ConfigureAwait(false);

            return result;
        }

        public async Task<object?> ExecuteScalarAsync(string sql, IDictionary<string, object?>? parameters = null)
        {
            await EnsureDatabaseExistsAsync().ConfigureAwait(false);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 120;

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

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 120;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            var dataTable = new DataTable();
            using var adapter = new SqlDataAdapter(command);
            adapter.Fill(dataTable);

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

                var builder = new SqlConnectionStringBuilder(_connectionString);
                var databaseName = builder.InitialCatalog;

                if (!string.IsNullOrWhiteSpace(databaseName))
                {
                    var masterBuilder = new SqlConnectionStringBuilder(_connectionString)
                    {
                        InitialCatalog = "master"
                    };

                    await using var connection = new SqlConnection(masterBuilder.ConnectionString);
                    await connection.OpenAsync().ConfigureAwait(false);

                    var commandText = "IF DB_ID(@databaseName) IS NULL BEGIN CREATE DATABASE [" + databaseName + "] END;";
                    using var command = new SqlCommand(commandText, connection);
                    command.Parameters.AddWithValue("@databaseName", databaseName);
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
                else if (!string.IsNullOrWhiteSpace(builder.AttachDBFilename))
                {
                    var databasePath = builder.AttachDBFilename;

                    if (!Path.IsPathRooted(databasePath))
                    {
                        databasePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, databasePath));
                    }

                    var directory = Path.GetDirectoryName(databasePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    if (!File.Exists(databasePath))
                    {
                        await using var connection = new SqlConnection(_connectionString);
                        await connection.OpenAsync().ConfigureAwait(false);
                    }
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
