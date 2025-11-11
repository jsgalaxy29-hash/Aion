namespace Aion.AppHost.Services.Navigation;

/// <summary>
/// Provides access to module metadata for the current user (filtered by RBAC).
/// </summary>
public interface IModuleCatalog
{
    /// <summary>
    /// Returns the authorized modules for the current user.
    /// </summary>
    Task<IReadOnlyCollection<ModuleSummary>> GetModulesAsync(CancellationToken ct = default);

    /// <summary>
    /// Tries to locate a module by its unique key.
    /// </summary>
    Task<ModuleSummary?> TryGetByKeyAsync(string moduleKey, CancellationToken ct = default);

    /// <summary>
    /// Tries to locate a module by its registered route.
    /// </summary>
    Task<ModuleSummary?> TryGetByRouteAsync(string route, CancellationToken ct = default);

    /// <summary>
    /// Executes a fuzzy search over module titles and aliases.
    /// </summary>
    Task<IReadOnlyCollection<ModuleSummary>> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default);

    /// <summary>
    /// Tries to match a user provided string to a module using fuzzy heuristics.
    /// </summary>
    Task<ModuleSummary?> TryMatchAsync(string input, CancellationToken ct = default);
}
