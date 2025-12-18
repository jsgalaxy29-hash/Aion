using System;
using System.Collections.Generic;
using System.Text.Json;
using Aion.Domain.UI;

namespace Aion.Domain.Services.Navigation;

public static class NavigationParameterParser
{
    public static IReadOnlyDictionary<string, object?> BuildDefaultParameters(string route, string? raw)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var routeParameters = RouteRegistry.ToParameters(route, null);
        if (routeParameters is not null)
        {
            foreach (var kvp in routeParameters)
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            return result;
        }

        var trimmed = raw.Trim();
        if (trimmed.StartsWith("#", StringComparison.Ordinal))
        {
            ParseKeyValuePairs(trimmed[1..], result);
            return result;
        }

        if (trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
        {
            ParseJsonObject(trimmed, result);
            return result;
        }

        ParseKeyValuePairs(trimmed.TrimStart('?'), result);
        return result;
    }

    private static void ParseJsonObject(string payload, IDictionary<string, object?> target)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            foreach (var property in doc.RootElement.EnumerateObject())
            {
                var key = RouteRegistry.NormalizeParameterKey(property.Name);
                target[key] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number when property.Value.TryGetInt64(out var l) => l,
                    JsonValueKind.Number => property.Value.GetDecimal(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Array => property.Value.ToString(),
                    JsonValueKind.Object => property.Value.ToString(),
                    _ => null
                };
            }
        }
        catch
        {
            // Silently ignore invalid JSON, the navigation command will still work without defaults.
        }
    }

    private static void ParseKeyValuePairs(string payload, IDictionary<string, object?> target)
    {
        var parts = payload.Split(new[] { '&', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            var pair = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (pair.Length == 0)
            {
                continue;
            }

            var key = RouteRegistry.NormalizeParameterKey(Uri.UnescapeDataString(pair[0]));
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            object? value = null;
            if (pair.Length > 1)
            {
                var rawValue = Uri.UnescapeDataString(pair[1]);
                if (bool.TryParse(rawValue, out var boolValue))
                {
                    value = boolValue;
                }
                else if (long.TryParse(rawValue, out var longValue))
                {
                    value = longValue;
                }
                else
                {
                    value = rawValue;
                }
            }
            else
            {
                value = true;
            }

            target[key] = value;
        }
    }
}
