using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Aion.DataEngine.Interfaces;

namespace Aion.DataEngine.Providers
{
    /// <summary>
    /// Implementation of <see cref="IDataProvider"/> for SQL Server.
    /// Uses ADO.NET's <see cref="SqlConnection"/> and <see cref="SqlCommand"/> to
    /// execute SQL statements.  Instances of this provider are intended to be
    /// short‑lived and thread‑safe.
    /// </summary>
    public class SqlServerDataProvider : IDataProvider
    {
        /// <summary>
        /// Initializes a new instance of the provider with the specified
        /// connection string.
        /// </summary>
        /// <param name="connectionString">The SQL Server connection string.</param>
        public SqlServerDataProvider(string connectionString)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <inheritdoc />
        public string ConnectionString { get; }

        /// <inheritdoc />
        public async Task<int> ExecuteNonQueryAsync(string commandText, IDictionary<string, object?>? parameters = null)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand(commandText, connection);
            AddParameters(command, parameters);
            return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<DataTable> ExecuteQueryAsync(string commandText, IDictionary<string, object?>? parameters = null)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand(commandText, connection);
            AddParameters(command, parameters);
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            var table = new DataTable();
            table.Load(reader);
            return table;
        }

        /// <inheritdoc />
        public async Task<IDbTransaction> BeginTransactionAsync()
        {
            var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            return await Task.FromResult(connection.BeginTransaction()).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds parameters to the provided SqlCommand.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="parameters">The parameters.</param>
        private static void AddParameters(SqlCommand command, IDictionary<string, object?>? parameters)
        {
            if (parameters == null) return;
            foreach (var kvp in parameters)
            {
                command.Parameters.AddWithValue(kvp.Key, kvp.Value ?? DBNull.Value);
            }
        }

        public async Task<object?> ExecuteScalarAsync(string sql, IDictionary<string, object?>? parameters = null)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand(sql, connection);
            AddParameters(command, parameters);
            return await command.ExecuteScalarAsync().ConfigureAwait(false);
        }
    }
}