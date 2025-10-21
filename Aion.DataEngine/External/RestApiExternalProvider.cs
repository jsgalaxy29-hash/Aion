using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Aion.DataEngine.Entities;
using Aion.DataEngine.Interfaces;

namespace Aion.DataEngine.External
{
    /// <summary>
    /// External source provider for REST API endpoints returning JSON.  This
    /// provider performs HTTP GET requests and deserializes the JSON into
    /// a collection of dictionaries.  Only read operations are supported.
    /// </summary>
    public class RestApiExternalProvider : IExternalSourceProvider
    {
        private readonly HttpClient _httpClient;
        public RestApiExternalProvider(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
        }
        public string Type => "REST_API";
        public async Task<IEnumerable<IDictionary<string, object?>>> ReadAsync(SSourceExterne source, SSourceBinding binding, IDictionary<string, object?>? context)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var urlBuilder = new StringBuilder();
            // Base URL may include path.  We assume ExtractionPath is a relative URI or null.
            urlBuilder.Append(source.Url?.TrimEnd('/') ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(binding.ExtractionPath))
            {
                var path = binding.ExtractionPath!.TrimStart('/');
                if (urlBuilder.Length > 0) urlBuilder.Append('/');
                urlBuilder.Append(path);
            }
            // Append context as query string
            if (context != null && context.Count > 0)
            {
                var queryStarted = false;
                foreach (var kvp in context)
                {
                    if (kvp.Value == null) continue;
                    var key = kvp.Key;
                    var value = kvp.Value!.ToString();
                    if (!queryStarted)
                    {
                        urlBuilder.Append("?");
                        queryStarted = true;
                    }
                    else
                    {
                        urlBuilder.Append("&");
                    }
                    urlBuilder.Append(Uri.EscapeDataString(key));
                    urlBuilder.Append("=");
                    urlBuilder.Append(Uri.EscapeDataString(value!));
                }
            }
            var url = urlBuilder.ToString();
            using var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var rows = new List<IDictionary<string, object?>>();
            // Expect array of objects
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in root.EnumerateArray())
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (var prop in element.EnumerateObject())
                    {
                        dict[prop.Name] = prop.Value.ValueKind == JsonValueKind.Null ? null : (object?)prop.Value.ToString();
                    }
                    rows.Add(dict);
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // Wrap single object in one row
                var dict = new Dictionary<string, object?>();
                foreach (var prop in root.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.ValueKind == JsonValueKind.Null ? null : (object?)prop.Value.ToString();
                }
                rows.Add(dict);
            }
            return rows;
        }
        public Task WriteAsync(SSourceExterne source, SSourceBinding binding, IEnumerable<IDictionary<string, object?>> rows)
        {
            // Writing back to a REST API is not supported in this provider
            throw new NotSupportedException("REST_API provider is read‑only");
        }
    }
}