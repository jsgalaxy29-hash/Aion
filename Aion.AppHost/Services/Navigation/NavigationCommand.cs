using Aion.Domain.UI.Navigation;

namespace Aion.AppHost.Services.Navigation;

/// <summary>
/// Represents a parsed command extracted from the command palette input.
/// </summary>
public sealed class NavigationCommand
{
    public NavigationCommand(NavigationCommandType type, ModuleSummary? module)
    {
        Type = type;
        Module = module;
    }

    public NavigationCommandType Type { get; }

    public ModuleSummary? Module { get; }
}
