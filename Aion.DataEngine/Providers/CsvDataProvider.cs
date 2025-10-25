using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aion.DataEngine.Interfaces;

namespace Aion.DataEngine.Providers
{
    /// <summary>
    /// Implements <see cref="IDataProvider"/> for CSV files.  This provider
    /// reads data from a CSV file into a <see cref="DataTable"/>.  Only
    /// read operations are supported; writes and transactions are not.
    /// </summary>
    public class CsvDataProvider : IDataProvider
    {
        private readonly string _filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvDataProvider"/>.
        /// </summary>
        /// <param name="filePath">Absolute or relative path to the CSV file.</param>
        public CsvDataProvider(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
            _filePath = filePath;
        }

        /// <inheritdoc />
        public string ConnectionString => _filePath;

        /// <inheritdoc />
        public Task<IDbTransaction> BeginTransactionAsync()
        {
            // Transactions are not supported for file providers.
            throw new NotSupportedException("Transactions are not supported by CsvDataProvider.");
        }

        /// <inheritdoc />
        public Task<int> ExecuteNonQueryAsync(string commandText, IDictionary<string, object?>? parameters = null)
        {
            // Writes are not supported.  This provider is read‑only.
            throw new NotSupportedException("ExecuteNonQueryAsync is not supported by CsvDataProvider.");
        }

        /// <inheritdoc />
        public async Task<DataTable> ExecuteQueryAsync(string commandText, IDictionary<string, object?>? parameters = null)
        {
            // Ignore commandText; always load from the configured file
            var fullPath = Path.GetFullPath(_filePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("CSV file not found.", fullPath);
            }
            var lines = await File.ReadAllLinesAsync(fullPath).ConfigureAwait(false);
            var table = new DataTable();
            if (lines.Length == 0) return table;
            var headers = lines[0].Split(',');
            foreach (var header in headers)
            {
                table.Columns.Add(header.Trim(), typeof(string));
            }
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var values = line.Split(',');
                var row = table.NewRow();
                for (int i = 0; i < headers.Length && i < values.Length; i++)
                {
                    row[headers[i]] = values[i];
                }
                table.Rows.Add(row);
            }
            return table;
        }

        public Task<object?> ExecuteScalarAsync(string sql, IDictionary<string, object?>? parameters = null)
        {
            throw new NotImplementedException();
        }
    }
}