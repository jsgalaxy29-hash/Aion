using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aion.AppHost.Services.Navigation;

/// <summary>
/// Utility responsible for converting icon identifiers stored in metadata into Fluent UI icon instances.
/// </summary>
internal static class FluentIconResolver
{
    private static readonly Regex IconPattern = new(
        "^(?<name>[A-Za-z0-9]+?)(?<size>\\d+)(?<style>Regular|Filled)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly ConcurrentDictionary<string, Icon> Cache = new(StringComparer.Ordinal);

    private static readonly Icon DefaultIcon = CreateOrFallback("TabDesktop20Regular")
        ?? new Microsoft.FluentUI.AspNetCore.Components.Icons.Regular.Size20.CalendarLtr();

    /// <summary>
    /// Resolves a Fluent <see cref="Icon"/> instance for the provided token, using a default icon if none can be found.
    /// </summary>
    /// <param name="token">Identifier coming from the metabase (e.g. "TabDesktop20Regular").</param>
    /// <param name="fallback">Optional fallback icon to use when the token cannot be resolved.</param>
    /// <returns>An icon instance suitable for Fluent components.</returns>
    public static Icon Resolve(string? token, Icon? fallback = null)
    {
        if (!string.IsNullOrWhiteSpace(token) && Cache.TryGetValue(token, out var cached))
        {
            return cached;
        }

        var icon = TryCreate(token) ?? fallback ?? DefaultIcon;

        if (!string.IsNullOrWhiteSpace(token))
        {
            Cache[token!] = icon;
        }

        return icon;
    }

    private static Icon? TryCreate(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var match = IconPattern.Match(token);
        if (!match.Success)
        {
            return null;
        }

        var style = match.Groups["style"].Value;
        var size = match.Groups["size"].Value;
        var name = match.Groups["name"].Value;
        var typeName = $"Microsoft.FluentUI.AspNetCore.Components.Icons.{style}.Size{size}.{name}";
        var assembly = typeof(FluentIcon).Assembly;
        var type = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
        if (type is null)
        {
            return null;
        }

        return Activator.CreateInstance(type) as Icon;
    }

    private static Icon? CreateOrFallback(string token)
        => TryCreate(token);
}
