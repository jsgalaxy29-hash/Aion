using System;
using System.Collections.Generic;
using System.Data;
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

        public SqlDataProvider(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AionDb")
                ?? throw new InvalidOperationException("Connection string 'AionDb' not found");
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, IDictionary<string, object?>? parameters = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 120; // 2 minutes pour les scripts complexes

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            int result = await command.ExecuteNonQueryAsync();

            return result;
        }

        public async Task<object?> ExecuteScalarAsync(string sql, IDictionary<string, object?>? parameters = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 120;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value ? null : result;
        }

        public async Task<DataTable> ExecuteQueryAsync(string sql, IDictionary<string, object?>? parameters = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

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
    }
}