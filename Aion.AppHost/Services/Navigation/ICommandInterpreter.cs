namespace Aion.AppHost.Services.Navigation;

/// <summary>
/// Parses natural language navigation commands.
/// </summary>
public interface ICommandInterpreter
{
    /// <summary>
    /// Attempts to interpret <paramref name="input"/> into a navigation command.
    /// </summary>
    /// <param name="input">User provided text.</param>
    /// <param name="catalog">Module catalog for fuzzy lookups.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<NavigationCommand?> TryInterpretAsync(string input, IModuleCatalog catalog, CancellationToken ct = default);
}
