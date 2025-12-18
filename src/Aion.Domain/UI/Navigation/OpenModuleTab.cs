using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Aion.Domain.UI.Navigation;

/// <summary>
/// Represents an opened module inside the navigation shell.
/// </summary>
public sealed class OpenModuleTab
{
    public OpenModuleTab(string moduleKey, string title, RenderFragment content, IReadOnlyDictionary<string, object?>? parameters, string? icon)
    {
        ModuleKey = moduleKey ?? throw new ArgumentNullException(nameof(moduleKey));
        Title = title;
        Content = content;
        Parameters = parameters is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(parameters, StringComparer.OrdinalIgnoreCase);
        Icon = icon;
    }

    public string ModuleKey { get; }

    public string Title { get; set; }

    public bool IsDirty { get; set; }

    public bool IsActive { get; set; }

    public RenderFragment Content { get; private set; }

    public IDictionary<string, object?> Parameters { get; }

    public string? Icon { get; }

    public void Update(RenderFragment content, IReadOnlyDictionary<string, object?> parameters)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Parameters.Clear();
        foreach (var kvp in parameters)
        {
            Parameters[kvp.Key] = kvp.Value;
        }
    }
}
