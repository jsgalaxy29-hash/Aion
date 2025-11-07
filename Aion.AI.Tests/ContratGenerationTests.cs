using System.Threading.Tasks;
using Aion.AI.Abstractions;
using Aion.AI.Extensions;
using Aion.AI.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aion.AI.Tests;

public class ContratGenerationTests
{
    [Fact]
    public async Task ContratScenarioProducesExpectedArtifacts()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddAionAi();
        var audit = new InMemoryAuditTrailService();
        services.AddSingleton<IAuditTrailService>(audit);

        await using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAionAiOrchestrator>();

        const string request = "Crée un module Contrats avec entités Contrat et Assure et génère les CRUD complets.";
        var result = await orchestrator.HandleNaturalLanguageRequestAsync(request);

        result.Success.Should().BeTrue();
        result.Plan.ModuleName.Should().Be("Contrats");
        result.Plan.Steps.Should().NotBeEmpty();
        result.PatchYaml.Should().Contain("FContrat");
        result.PatchYaml.Should().NotContain("_");
        result.Artifacts.Artifacts.Should().ContainSingle(a => a.RelativePath.EndsWith("Contrat.cs"));
        audit.Record.Should().NotBeNull();
        audit.Record!.Status.Should().Be(GenerationStatus.Applied);
    }

    private sealed class InMemoryAuditTrailService : IAuditTrailService
    {
        public AuditRecord? Record { get; private set; }

        public Task RecordAsync(AuditRecord record, System.Threading.CancellationToken ct = default)
        {
            Record = record;
            return Task.CompletedTask;
        }
    }
}
