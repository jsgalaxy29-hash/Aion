using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.Contracts;

namespace Aion.Infrastructure.Services
{
    /// <summary>
    /// Résolveur simple de DataQueryRef. Permet d'exécuter des requêtes EF nommées, des appels API internes et des rapports.
    /// Cette implémentation illustre le principe mais n'est pas exhaustive.
    /// </summary>
    public sealed class DataQueryResolver : IDataQueryResolver
    {
        private readonly AionDbContext _db;
        private readonly IHttpClientFactory _clientFactory;

        // Catalogue de requêtes EF nominatives. Key = nom après "ef:".
        private readonly IDictionary<string, Func<IDictionary<string, object?>?, CancellationToken, Task<object?>>> _efQueries;

        public DataQueryResolver(AionDbContext db, IHttpClientFactory clientFactory)
        {
            _db = db;
            _clientFactory = clientFactory;
            _efQueries = new Dictionary<string, Func<IDictionary<string, object?>?, CancellationToken, Task<object?>>>(StringComparer.OrdinalIgnoreCase)
            {
                // Exemple : retourne la liste des derniers modules
                ["LatestModules"] = async (settings, ct) =>
                {
                    var top = settings != null && settings.TryGetValue("count", out var v) && v is int n ? n : 5;
                    return await Task.FromResult(_db.SModule.OrderByDescending(m => m.Id).Take(top).Select(m => new { m.Code, m.Name }).ToList());
                }
            };
        }

        public async Task<object?> ExecuteAsync(string dataQueryRef, IDictionary<string, object?>? settings, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dataQueryRef)) return null;
            var parts = dataQueryRef.Split(':', 2);
            if (parts.Length != 2) return null;
            var scheme = parts[0];
            var key = parts[1];
            switch (scheme.ToLowerInvariant())
            {
                case "ef":
                    if (_efQueries.TryGetValue(key, out var query))
                    {
                        return await query(settings, ct);
                    }
                    break;
                case "api":
                    {
                        // Appel HTTP GET vers un endpoint interne (fictive). Exemple: api:/reports/kpi/sales?period=YTD
                        var client = _clientFactory.CreateClient();
                        var url = key;
                        var response = await client.GetAsync(url, ct);
                        response.EnsureSuccessStatusCode();
                        var content = await response.Content.ReadAsStringAsync(ct);
                        return JsonSerializer.Deserialize<object>(content);
                    }
                case "report":
                    {
                        // Exemple: résout un rapport. Ici on retourne un message simple.
                        return await Task.FromResult(new { ReportName = key, GeneratedAt = DateTimeOffset.UtcNow });
                    }
            }
            return null;
        }

    }
}
