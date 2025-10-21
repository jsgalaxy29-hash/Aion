using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Aion.DataEngine.Interfaces;

namespace Aion.DataEngine.Providers
{
    /// <summary>
    /// Implements <see cref="IDataProvider"/> for RESTful API endpoints.  This
    /// provider performs HTTP requests to retrieve data in JSON format and
    /// converts it into a <see cref="DataTable"/>.  Only read operations are
    /// supported.  Non‑query operations and transactions are not supported.
    /// </summary>
    public class ApiDataProvider : IDataProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiDataProvider"/>.
        /// </summary>
        /// <param name="baseUrl">The base URL of the API (e.g. "https://api.example.com").</param>
        /// <param name="httpClient">Optional <see cref="HttpClient"/> instance.  If null, a new instance is created.</param>
        public ApiDataProvider(string baseUrl, HttpClient? httpClient = null)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) throw new ArgumentNullException(nameof(baseUrl));
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = httpClient ?? new HttpClient();
        }

        /// <inheritdoc />
        public string ConnectionString => _baseUrl;

        /// <inheritdoc />
        public Task<IDbTransaction> BeginTransactionAsync()
        {
            // Transactions are not supported over HTTP.
            throw new NotSupportedException("Transactions are not supported by ApiDataProvider.");
        }

        /// <inheritdoc />
        public async Task<int> ExecuteNonQueryAsync(string commandText, IDictionary<string, object?>? parameters = null)
        {
            // Non‑query operations are not supported; the API is read‑only.
            throw new NotSupportedException("ExecuteNonQueryAsync is not supported by ApiDataProvider.");
        }

        /// <inheritdoc />
        public async Task<DataTable> ExecuteQueryAsync(string commandText, IDictionary<string, object?>? parameters = null)
        {
            if (string.IsNullOrWhiteSpace(commandText)) throw new ArgumentNullException(nameof(commandText));
            // Build the full URL.  If commandText starts with http, use it as is; otherwise append to baseUrl.
            var url = commandText.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? commandText : $"{_baseUrl}/{commandText.TrimStart('/')}";
            // Append query parameters if provided
            if (parameters != null && parameters.Count > 0)
            {
                var query = new StringBuilder();
                foreach (var kvp in parameters)
                {
                    if (kvp.Value == null) continue;
                    if (query.Length == 0)
                        query.Append("?");
                    else
                        query.Append("&");
                    query.Append(Uri.EscapeDataString(kvp.Key.TrimStart('@')));
                    query.Append("=");
                    query.Append(Uri.EscapeDataString(kvp.Value.ToString()!));
                }
                url += query.ToString();
            }
            using var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            // Deserialize JSON into a DataTable.  Expecting an array of objects.
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var table = new DataTable();
            if (root.ValueKind != JsonValueKind.Array)
            {
                // If the root is a single object, wrap it in an array for consistency.
                var newArray = new List<JsonElement> { root };
                root = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(newArray));
            }
            // Populate columns based on first element
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                var first = root[0];
                foreach (var prop in first.EnumerateObject())
                {
                    table.Columns.Add(prop.Name, typeof(string));
                }
                // Add rows
                foreach (var element in root.EnumerateArray())
                {
                    var row = table.NewRow();
                    foreach (var prop in element.EnumerateObject())
                    {
                        row[prop.Name] = prop.Value.ToString();
                    }
                    table.Rows.Add(row);
                }
            }
            return table;
        }
    }
}