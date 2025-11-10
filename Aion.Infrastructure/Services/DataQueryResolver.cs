using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Aion.Infrastructure.Options;

namespace Aion.Infrastructure.Services
{
    /// <summary>
    /// Résolveur simple de DataQueryRef. Permet d'exécuter des requêtes EF nommées, des appels API internes et des rapports.
    /// Cette implémentation illustre le principe mais n'est pas exhaustive.
    /// </summary>
    public sealed class DataQueryResolver : IDataQueryResolver
    {
        private readonly IDbContextFactory<AionDbContext> _dbFactory;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IReadOnlyList<Uri> _allowedApiBaseUris;

        // Catalogue de requêtes EF nominatives. Key = nom après "ef:".
        private readonly IDictionary<string, Func<IDictionary<string, object?>?, CancellationToken, Task<object?>>> _efQueries;

        public DataQueryResolver(IDbContextFactory<AionDbContext> dbFactory, IHttpClientFactory clientFactory, IOptions<DataQueryResolverOptions> options)
        {
            _dbFactory = dbFactory;
            _clientFactory = clientFactory;
            var optionValues = options?.Value ?? new DataQueryResolverOptions();
            _allowedApiBaseUris = optionValues.AllowedApiBaseUrls
                .Select(TryNormalizeBaseUri)
                .Where(static uri => uri is not null)
                .Select(static uri => uri!)
                .ToArray();
            _efQueries = new Dictionary<string, Func<IDictionary<string, object?>?, CancellationToken, Task<object?>>>(StringComparer.OrdinalIgnoreCase)
            {
                // Exemple : retourne la liste des derniers modules
                ["LatestModules"] = async (settings, ct) =>
                {
                    var top = settings != null && settings.TryGetValue("count", out var v) && v is int n ? n : 5;
                    await using var db = await _dbFactory.CreateDbContextAsync(ct);
                    return await db.SModule
                        .OrderByDescending(m => m.Id)
                        .Take(top)
                        .Select(m => new { V = m.Id.ToString(), m.Name })
                        .ToListAsync(ct);
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
                        // Appel HTTP GET vers un endpoint interne autorisé. Exemple: api:/reports/kpi/sales?period=YTD
                        var targetUri = ResolveApiUri(key);
                        var client = _clientFactory.CreateClient();
                        using var response = await client.GetAsync(targetUri, ct).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();
                        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
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

        private Uri ResolveApiUri(string key)
        {
            if (_allowedApiBaseUris.Count == 0)
            {
                throw new InvalidOperationException("API queries are disabled because no allowed base URL is configured.");
            }

            if (!Uri.TryCreate(key, UriKind.RelativeOrAbsolute, out var requestedUri))
            {
                throw new InvalidOperationException($"Invalid API query reference '{key}'.");
            }

            if (!requestedUri.IsAbsoluteUri)
            {
                return new Uri(_allowedApiBaseUris[0], requestedUri);
            }

            foreach (var baseUri in _allowedApiBaseUris)
            {
                if (baseUri.IsBaseOf(requestedUri) || Uri.Compare(baseUri, requestedUri, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return requestedUri;
                }
            }

            throw new InvalidOperationException($"API endpoint '{requestedUri}' is not allowed.");
        }

        private static Uri? TryNormalizeBaseUri(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                return null;
            }

            if (!uri.IsAbsoluteUri)
            {
                return null;
            }

            if (!uri.AbsolutePath.EndsWith("/", StringComparison.Ordinal))
            {
                uri = new Uri(uri, uri.AbsolutePath + "/");
            }

            return uri;
        }

    }
}
