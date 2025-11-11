using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Aion.AppHost.Services.Navigation;

/// <summary>
/// Lightweight view over a module metadata entry retrieved from the metabase.
/// </summary>
public sealed class ModuleSummary
{
    public ModuleSummary(
        string key,
        string title,
        string? description,
        string route,
        string? icon,
        string group,
        int moduleId,
        int menuId,
        IReadOnlyDictionary<string, object?>? defaultParameters)
    {
        Key = key;
        Title = title;
        Description = description;
        Route = NormalizeRoute(route);
        Icon = icon;
        Group = group;
        ModuleId = moduleId;
        MenuId = menuId;
        DefaultParameters = defaultParameters ?? new Dictionary<string, object?>();

        NormalizedTitle = Normalize(title);
        NormalizedKey = Normalize(key);
        NormalizedDescription = Normalize(description ?? string.Empty);
    }

    /// <summary>
    /// Unique key for the module, mapped to SModule.Name.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Display title for UI components.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Optional description displayed in module catalog views.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Route registered in <see cref="Aion.Domain.UI.RouteRegistry"/>.
    /// </summary>
    public string Route { get; }

    /// <summary>
    /// Fluent icon identifier.
    /// </summary>
    public string? Icon { get; }

    /// <summary>
    /// Logical group (top level menu).
    /// </summary>
    public string Group { get; }

    public int ModuleId { get; }

    public int MenuId { get; }

    /// <summary>
    /// Default parameters provided by the menu configuration.
    /// </summary>
    public IReadOnlyDictionary<string, object?> DefaultParameters { get; }

    internal string NormalizedTitle { get; }

    internal string NormalizedKey { get; }

    internal string NormalizedDescription { get; }

    private static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var normalized = input.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    private static string NormalizeRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return "/";
        }

        var trimmed = route.Trim();
        if (!trimmed.StartsWith('/'))
        {
            trimmed = "/" + trimmed;
        }

        return trimmed;
    }
}
