using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aion.AppHost.Services.Navigation;
using FluentAssertions;
using Xunit;

namespace Aion.AppHost.Tests;

public sealed class SimpleCommandInterpreterTests
{
    private static readonly ModuleSummary[] Modules =
    {
        new ModuleSummary("ListDyn", "Liste dynamique", "Gestion des listes", "/dynamic/list", "Apps20Regular", "Administration", 1, 10, new Dictionary<string, object?>()),
        new ModuleSummary("Rights", "Gestion des droits", "Droits utilisateurs", "/admin/rights", "ShieldCheckmark20Regular", "Administration", 2, 11, new Dictionary<string, object?>())
    };

    private readonly SimpleCommandInterpreter _interpreter = new();
    private readonly FakeCatalog _catalog = new(Modules);

    [Theory]
    [InlineData("ouvre liste dynamique", "ListDyn", NavigationCommandType.OpenModule)]
    [InlineData("Ouvrir le module gestion des droits", "Rights", NavigationCommandType.OpenModule)]
    [InlineData("ferme gestion des droits", "Rights", NavigationCommandType.CloseModule)]
    [InlineData("Ouvre : gestion des droits!", "Rights", NavigationCommandType.OpenModule)]
    public async Task Should_parse_module_commands(string input, string expectedKey, NavigationCommandType type)
    {
        var command = await _interpreter.TryInterpretAsync(input, _catalog, CancellationToken.None);

        command.Should().NotBeNull();
        command!.Type.Should().Be(type);
        command.Module.Should().NotBeNull();
        command.Module!.Key.Should().Be(expectedKey);
    }

    [Fact]
    public async Task Should_detect_close_all()
    {
        var command = await _interpreter.TryInterpretAsync("ferme tous les modules", _catalog, CancellationToken.None);

        command.Should().NotBeNull();
        command!.Type.Should().Be(NavigationCommandType.CloseAll);
        command.Module.Should().BeNull();
    }

    [Fact]
    public async Task Should_return_null_for_unknown_text()
    {
        var command = await _interpreter.TryInterpretAsync("statistiques", _catalog, CancellationToken.None);
        command.Should().BeNull();
    }

    private sealed class FakeCatalog : IModuleCatalog
    {
        private readonly IReadOnlyCollection<ModuleSummary> _modules;

        public FakeCatalog(IReadOnlyCollection<ModuleSummary> modules)
        {
            _modules = modules;
        }

        public Task<IReadOnlyCollection<ModuleSummary>> GetModulesAsync(CancellationToken ct = default)
            => Task.FromResult(_modules);

        public Task<ModuleSummary?> TryGetByKeyAsync(string moduleKey, CancellationToken ct = default)
            => Task.FromResult(_modules.FirstOrDefault(m => string.Equals(m.Key, moduleKey, StringComparison.OrdinalIgnoreCase)));

        public Task<ModuleSummary?> TryGetByRouteAsync(string route, CancellationToken ct = default)
            => Task.FromResult(_modules.FirstOrDefault(m => string.Equals(m.Route, route, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyCollection<ModuleSummary>> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyCollection<ModuleSummary>)_modules);

        public Task<ModuleSummary?> TryMatchAsync(string input, CancellationToken ct = default)
        {
            var normalized = input?.Trim() ?? string.Empty;
            var match = _modules.FirstOrDefault(m => m.Title.Contains(normalized, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(match);
        }
    }
}
