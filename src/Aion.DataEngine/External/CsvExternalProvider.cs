using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aion.DataEngine.Entities;
using Aion.DataEngine.Interfaces;

namespace Aion.DataEngine.External
{
    /// <summary>
    /// External source provider for CSV files.  This provider reads from
    /// and writes to CSV files stored on the local file system.  It
    /// expects the first row to contain column names.  The delimiter is
    /// determined from the source's Params JSON (key: "delimiter").
    /// </summary>
    public class CsvExternalProvider : IExternalSourceProvider
    {
        public string Type => "CSV";
        public async Task<IEnumerable<IDictionary<string, object?>>> ReadAsync(SSourceExterne source, SSourceBinding binding, IDictionary<string, object?>? context)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrWhiteSpace(source.Url))
            {
                throw new InvalidOperationException("CSV source requires a file path in Url");
            }
            var filePath = source.Url;
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"CSV file not found: {filePath}");
            }
            // Determine delimiter (default to ';')
            char delimiter = ';';
            try
            {
                if (!string.IsNullOrWhiteSpace(source.Params))
                {
                    var json = JsonDocument.Parse(source.Params);
                    if (json.RootElement.TryGetProperty("delimiter", out var delProp))
                    {
                        var delimStr = delProp.GetString();
                        if (!string.IsNullOrEmpty(delimStr))
                        {
                            delimiter = delimStr[0];
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // ignore invalid Params JSON
            }
            var rows = new List<IDictionary<string, object?>>();
            using var reader = new StreamReader(filePath);
            // Read header
            var headerLine = await reader.ReadLineAsync().ConfigureAwait(false);
            if (headerLine == null) return rows;
            var headers = headerLine.Split(delimiter).Select(h => h.Trim()).ToArray();
            string? line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                var values = line.Split(delimiter);
                var dict = new Dictionary<string, object?>();
                for (int i = 0; i < headers.Length && i < values.Length; i++)
                {
                    dict[headers[i]] = values[i];
                }
                rows.Add(dict);
            }
            return rows;
        }
        public async Task WriteAsync(SSourceExterne source, SSourceBinding binding, IEnumerable<IDictionary<string, object?>> rows)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrWhiteSpace(source.Url))
            {
                throw new InvalidOperationException("CSV source requires a file path in Url");
            }
            var filePath = source.Url;
            // Determine delimiter (default ';')
            char delimiter = ';';
            try
            {
                if (!string.IsNullOrWhiteSpace(source.Params))
                {
                    var json = JsonDocument.Parse(source.Params);
                    if (json.RootElement.TryGetProperty("delimiter", out var delProp))
                    {
                        var delimStr = delProp.GetString();
                        if (!string.IsNullOrEmpty(delimStr))
                        {
                            delimiter = delimStr[0];
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // ignore invalid Params JSON
            }
            // Convert to list for enumeration
            var rowList = rows.ToList();
            if (rowList.Count == 0) return;
            // Determine header order: union of all keys across rows
            var headers = rowList.SelectMany(r => r.Keys).Distinct().ToList();
            using var writer = new StreamWriter(filePath, false); // overwrite existing file
            // Write header
            await writer.WriteLineAsync(string.Join(delimiter, headers)).ConfigureAwait(false);
            foreach (var row in rowList)
            {
                var line = string.Join(delimiter, headers.Select(h => row.TryGetValue(h, out var val) ? (val?.ToString() ?? string.Empty) : string.Empty));
                await writer.WriteLineAsync(line).ConfigureAwait(false);
            }
        }
    }
}