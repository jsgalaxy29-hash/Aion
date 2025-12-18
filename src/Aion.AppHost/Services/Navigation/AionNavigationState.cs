using System;
using System.Collections.Generic;
using System.Linq;
using Aion.Domain.UI.Navigation;

namespace Aion.AppHost.Services.Navigation;

/// <summary>
/// Holds per-user navigation state (open tabs, recents, favorites).
/// Scoped lifetime ensures isolation between Blazor circuits.
/// </summary>
public sealed class AionNavigationState
{
    private readonly List<OpenModuleTab> _tabs = new();
    private readonly LinkedList<string> _recentModules = new();
    private readonly HashSet<string> _favoriteModules = new(StringComparer.OrdinalIgnoreCase);
    private const int MaxRecent = 10;

    /// <summary>
    /// Raised when the list of tabs changes.
    /// </summary>
    public event EventHandler? TabsChanged;

    /// <summary>
    /// Raised when the active tab changes.
    /// </summary>
    public event EventHandler<string?>? ActiveTabChanged;

    /// <summary>
    /// Gets the currently active module key, if any.
    /// </summary>
    public string? ActiveModuleKey { get; private set; }

    /// <summary>
    /// Gets an immutable snapshot of the open tabs.
    /// </summary>
    public IReadOnlyList<OpenModuleTab> Tabs => _tabs;

    /// <summary>
    /// Gets the module keys recently opened by the user (most recent first).
    /// </summary>
    public IReadOnlyList<string> RecentModuleKeys => _recentModules.ToList();

    /// <summary>
    /// Marks a module as favourite for the current session.
    /// </summary>
    public void ToggleFavorite(string moduleKey)
    {
        if (_favoriteModules.Contains(moduleKey))
        {
            _favoriteModules.Remove(moduleKey);
        }
        else
        {
            _favoriteModules.Add(moduleKey);
        }

        // Notifies so UI can refresh icons.
        TabsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Checks if the module is part of the favourites collection.
    /// </summary>
    public bool IsFavorite(string moduleKey) => _favoriteModules.Contains(moduleKey);

    internal void AddOrActivate(OpenModuleTab tab)
    {
        var existing = _tabs.FirstOrDefault(t => string.Equals(t.ModuleKey, tab.ModuleKey, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            SetActive(existing.ModuleKey);
            return;
        }

        _tabs.Add(tab);
        SetActive(tab.ModuleKey);
        TabsChanged?.Invoke(this, EventArgs.Empty);
    }

    internal void Remove(string moduleKey)
    {
        var removed = _tabs.RemoveAll(t => string.Equals(t.ModuleKey, moduleKey, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            if (string.Equals(ActiveModuleKey, moduleKey, StringComparison.OrdinalIgnoreCase))
            {
                var newActive = _tabs.LastOrDefault();
                ActiveModuleKey = newActive?.ModuleKey;
                if (ActiveModuleKey is not null)
                {
                    newActive!.IsActive = true;
                }
            }

            TabsChanged?.Invoke(this, EventArgs.Empty);
            ActiveTabChanged?.Invoke(this, ActiveModuleKey);
        }
    }

    internal void Clear()
    {
        if (_tabs.Count == 0)
        {
            return;
        }

        _tabs.Clear();
        ActiveModuleKey = null;
        TabsChanged?.Invoke(this, EventArgs.Empty);
        ActiveTabChanged?.Invoke(this, ActiveModuleKey);
    }

    internal void SetActive(string? moduleKey)
    {
        if (moduleKey is null)
        {
            ActiveModuleKey = null;
            foreach (var tab in _tabs)
            {
                tab.IsActive = false;
            }
            ActiveTabChanged?.Invoke(this, ActiveModuleKey);
            return;
        }

        var matched = _tabs.FirstOrDefault(t => string.Equals(t.ModuleKey, moduleKey, StringComparison.OrdinalIgnoreCase));
        if (matched is null)
        {
            return;
        }

        foreach (var tab in _tabs)
        {
            tab.IsActive = string.Equals(tab.ModuleKey, moduleKey, StringComparison.OrdinalIgnoreCase);
        }

        ActiveModuleKey = matched.ModuleKey;
        TrackRecent(matched.ModuleKey);
        ActiveTabChanged?.Invoke(this, ActiveModuleKey);
        TabsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TrackRecent(string moduleKey)
    {
        var existing = _recentModules.FirstOrDefault(k => string.Equals(k, moduleKey, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            _recentModules.Remove(existing);
        }

        _recentModules.AddFirst(moduleKey);

        while (_recentModules.Count > MaxRecent)
        {
            _recentModules.RemoveLast();
        }
    }
}
