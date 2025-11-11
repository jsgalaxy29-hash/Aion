using Aion.AppHost.Services.Navigation;
using Aion.Domain.Services.Navigation;
using FluentAssertions;
using Xunit;

namespace Aion.AppHost.Tests;

public sealed class ModuleCatalogTests
{
    [Theory]
    [InlineData("module", "module", 0)]
    [InlineData("module", "modul", 1)]
    [InlineData("aion", "anion", 1)]
    [InlineData("navigation", "nation", 4)]
    public void Levenshtein_distance_should_match_expected(string source, string target, int expected)
    {
        ModuleCatalog.LevenshteinDistance(source, target).Should().Be(expected);
        ModuleCatalog.LevenshteinDistance(target, source).Should().Be(expected);
    }
}
