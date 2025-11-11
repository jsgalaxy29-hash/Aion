using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aion.DataEngine.Entities;
using Aion.DataEngine.Interfaces;
using Aion.Domain.Contracts;
using Aion.Domain.UI.Navigation;

namespace Aion.Domain.Services.Navigation;

/// <summary>
/// Loads module metadata from the metabase and exposes a filtered catalog for UI components.
/// </summary>
public sealed class ModuleCatalog : IModuleCatalog, IDisposable
{
    private readonly IMenuProvider _menuProvider;
    private readonly IUserContext _userContext;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    private IReadOnlyList<ModuleSummary>? _modules;
    private Dictionary<string, ModuleSummary>? _modulesByKey;
    private Dictionary<string, ModuleSummary>? _modulesByRoute;

    public ModuleCatalog(IMenuProvider menuProvider, IUserContext userContext)
    {
        _menuProvider = menuProvider;
        _userContext = userContext;
    }

    public void Dispose()
    {
        _initializationLock.Dispose();
    }

    public async Task<IReadOnlyCollection<ModuleSummary>> GetModulesAsync(CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        return _modules ?? Array.Empty<ModuleSummary>();
    }

    public async Task<ModuleSummary?> TryGetByKeyAsync(string moduleKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(moduleKey))
        {
            return null;
        }

        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        return _modulesByKey is not null && _modulesByKey.TryGetValue(moduleKey, out var module)
            ? module
            : null;
    }

    public async Task<ModuleSummary?> TryGetByRouteAsync(string route, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return null;
        }

        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        var normalizedRoute = NormalizeRoute(route);
        return _modulesByRoute is not null && _modulesByRoute.TryGetValue(normalizedRoute, out var module)
            ? module
            : null;
    }

    public async Task<IReadOnlyCollection<ModuleSummary>> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        if (_modules is null || _modules.Count == 0)
        {
            return Array.Empty<ModuleSummary>();
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return _modules.Take(maxResults).ToList();
        }

        var normalized = NormalizeForSearch(query);
        var scored = _modules
            .Select(module => (module, score: ComputeScore(module, normalized)))
            .Where(tuple => tuple.score < int.MaxValue)
            .OrderBy(tuple => tuple.score)
            .ThenBy(tuple => tuple.module.Title)
            .Take(maxResults)
            .Select(tuple => tuple.module)
            .ToList();

        return scored;
    }

    public async Task<ModuleSummary?> TryMatchAsync(string input, CancellationToken ct = default)
    {
        var results = await SearchAsync(input, 1, ct).ConfigureAwait(false);
        return results.FirstOrDefault();
    }

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (_modules is not null)
        {
            return;
        }

        await _initializationLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_modules is not null)
            {
                return;
            }

            var menus = await _menuProvider.GetAuthorizedMenuAsync(_userContext.TenantId, _userContext.UserId, ct).ConfigureAwait(false);
            if (menus is null || menus.Count == 0)
            {
                _modules = Array.Empty<ModuleSummary>();
                _modulesByKey = new Dictionary<string, ModuleSummary>(StringComparer.OrdinalIgnoreCase);
                _modulesByRoute = new Dictionary<string, ModuleSummary>(StringComparer.OrdinalIgnoreCase);
                return;
            }

            var menusById = new Dictionary<int, SMenu>();
            foreach (var menu in menus)
            {
                menusById[menu.Id] = menu;
            }

            var moduleMap = new Dictionary<string, ModuleSummary>(StringComparer.OrdinalIgnoreCase);
            var routeMap = new Dictionary<string, ModuleSummary>(StringComparer.OrdinalIgnoreCase);

            foreach (var menu in menus)
            {
                if (menu.Module is null || string.IsNullOrWhiteSpace(menu.Module.Route))
                {
                    continue;
                }

                var key = menu.Module.Name;
                if (string.IsNullOrWhiteSpace(key) || moduleMap.ContainsKey(key))
                {
                    continue;
                }

                var title = string.IsNullOrWhiteSpace(menu.Libelle)
                    ? menu.Module.Description ?? menu.Module.Name
                    : menu.Libelle;

                var description = string.IsNullOrWhiteSpace(menu.Module.Description)
                    ? menu.Libelle
                    : menu.Module.Description;

                var group = ResolveGroup(menu, menusById);
                var defaults = NavigationParameterParser.BuildDefaultParameters(menu.Module.Route, menu.Parametre);
                var summary = new ModuleSummary(
                    key,
                    title ?? key,
                    description,
                    menu.Module.Route,
                    menu.Icon ?? menu.Module.Icon,
                    group,
                    menu.Module.Id,
                    menu.Id,
                    defaults);

                moduleMap[key] = summary;
                routeMap[NormalizeRoute(summary.Route)] = summary;
            }

            _modules = moduleMap.Values
                .OrderBy(m => m.Group)
                .ThenBy(m => m.Title)
                .ToList();

            _modulesByKey = moduleMap;
            _modulesByRoute = routeMap;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private static string ResolveGroup(SMenu menu, IReadOnlyDictionary<int, SMenu> menusById)
    {
        var current = menu;
        while (current.ParentId.HasValue && menusById.TryGetValue(current.ParentId.Value, out var parent))
        {
            if (!parent.ParentId.HasValue)
            {
                return parent.Libelle;
            }

            current = parent;
        }

        return current.ParentId.HasValue ? current.Libelle : current.Libelle ?? "Modules";
    }

    private static string NormalizeRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return "/";
        }

        var trimmed = route.Trim();
        var separator = trimmed.IndexOfAny(new[] { '?', '#' });
        if (separator >= 0)
        {
            trimmed = trimmed[..separator];
        }

        if (!trimmed.StartsWith('/'))
        {
            trimmed = "/" + trimmed;
        }

        if (trimmed.Length > 1 && trimmed.EndsWith('/'))
        {
            trimmed = trimmed.TrimEnd('/');
        }

        return trimmed;
    }

    private static string NormalizeForSearch(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = text.Normalize(NormalizationForm.FormD);
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

    private static int ComputeScore(ModuleSummary module, string normalizedQuery)
    {
        if (string.IsNullOrEmpty(normalizedQuery))
        {
            return 0;
        }

        if (module.NormalizedKey == normalizedQuery)
        {
            return 0;
        }

        if (module.NormalizedTitle == normalizedQuery)
        {
            return 1;
        }

        if (module.NormalizedTitle.StartsWith(normalizedQuery, StringComparison.Ordinal))
        {
            return 2;
        }

        if (module.NormalizedTitle.Contains(normalizedQuery, StringComparison.Ordinal))
        {
            return 3;
        }

        if (module.NormalizedDescription.Contains(normalizedQuery, StringComparison.Ordinal))
        {
            return 4;
        }

        var distance = LevenshteinDistance(module.NormalizedTitle, normalizedQuery);
        return 10 + distance;
    }

    internal static int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
        {
            return target.Length;
        }

        if (string.IsNullOrEmpty(target))
        {
            return source.Length;
        }

        var rows = source.Length + 1;
        var cols = target.Length + 1;
        var matrix = new int[rows, cols];

        for (var i = 0; i < rows; i++)
        {
            matrix[i, 0] = i;
        }

        for (var j = 0; j < cols; j++)
        {
            matrix[0, j] = j;
        }

        for (var i = 1; i < rows; i++)
        {
            for (var j = 1; j < cols; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[rows - 1, cols - 1];
    }
}
