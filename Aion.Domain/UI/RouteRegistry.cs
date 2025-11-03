using System;
using System.Collections.Generic;

namespace Aion.Domain.UI
{
    /// <summary>
    /// Résout une route logique vers un Type de composant Blazor.
    /// Les modules appellent Register au démarrage pour publier leurs écrans.
    /// </summary>
    public static class RouteRegistry
    {
        private static readonly Dictionary<string, Type> _map = new(StringComparer.OrdinalIgnoreCase);

        public static void Register(string route, Type componentType)
        {
            if (componentType is null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            var key = NormalizeRoute(route);
            _map[key] = componentType;
        }

        public static Type Resolve(string route)
        {
            var key = NormalizeRoute(route);
            return _map.TryGetValue(key, out var componentType) ? componentType : typeof(object);
        }

        public static IReadOnlyDictionary<string, object?>? ToParameters(string? route, IDictionary<string, object?>? parameters)
        {
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    if (string.IsNullOrWhiteSpace(kvp.Key))
                    {
                        continue;
                    }

                    var key = NormalizeParameterKey(kvp.Key);
                    result[key] = kvp.Value;
                }
            }

            foreach (var kvp in ParseQueryParameters(route))
            {
                if (!result.ContainsKey(kvp.Key))
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return result;
        }

        public static string NormalizeParameterKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return key;
            }

            var trimmed = key.Trim().TrimStart('#');
            return trimmed.ToLowerInvariant() switch
            {
                "tablename" or "table" => "TableName",
                "rowid" => "RowId",
                "primarykey" => "PrimaryKey",
                _ => char.ToUpperInvariant(trimmed[0]) + trimmed[1..]
            };
        }

        private static string NormalizeRoute(string? route)
        {
            if (string.IsNullOrWhiteSpace(route))
            {
                return "/";
            }

            var trimmed = route.Trim();
            var separatorIndex = trimmed.IndexOfAny(new[] { '?', '#' });
            if (separatorIndex >= 0)
            {
                trimmed = trimmed[..separatorIndex];
            }

            if (!trimmed.StartsWith("/", StringComparison.Ordinal))
            {
                trimmed = "/" + trimmed;
            }

            if (trimmed.Length > 1 && trimmed.EndsWith("/", StringComparison.Ordinal))
            {
                trimmed = trimmed.TrimEnd('/');
            }

            return trimmed;
        }

        private static IDictionary<string, object?> ParseQueryParameters(string? route)
        {
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(route))
            {
                return result;
            }

            var questionIndex = route.IndexOf('?', StringComparison.Ordinal);
            if (questionIndex < 0)
            {
                return result;
            }

            var fragmentIndex = route.IndexOf('#', questionIndex);
            var query = fragmentIndex >= 0
                ? route.Substring(questionIndex + 1, fragmentIndex - questionIndex - 1)
                : route[(questionIndex + 1)..];

            if (string.IsNullOrWhiteSpace(query))
            {
                return result;
            }

            var segments = query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var segment in segments)
            {
                var kvp = segment.Split('=', 2, StringSplitOptions.TrimEntries);
                if (kvp.Length == 0)
                {
                    continue;
                }

                var key = NormalizeParameterKey(Uri.UnescapeDataString(kvp[0].Replace('+', ' ')));
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                var value = kvp.Length > 1
                    ? Uri.UnescapeDataString(kvp[1].Replace('+', ' '))
                    : "true";

                result[key] = value;
            }

            return result;
        }
    }
}
