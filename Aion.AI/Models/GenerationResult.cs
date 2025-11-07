using System.Collections.Generic;

namespace Aion.AI.Models;

/// <summary>
/// Summary returned to the caller after running the orchestrator.
/// </summary>
public sealed class GenerationResult
{
    public bool Success { get; init; }

    public GenerationPlan Plan { get; init; } = new();

    public string PatchYaml { get; init; } = string.Empty;

    public SimulationResult Simulation { get; init; } = new();

    public ArtifactGenerationResult Artifacts { get; init; } = new();

    public IList<string> Warnings { get; init; } = new List<string>();
}
