using System;
using System.Collections.Generic;

namespace Aion.AI.Models;

/// <summary>
/// Represents the orchestration plan that the engine will execute.
/// </summary>
public sealed class GenerationPlan
{
    public Guid PlanId { get; init; } = Guid.NewGuid();

    public string ModuleName { get; init; } = string.Empty;

    public IList<GenerationPlanStep> Steps { get; init; } = new List<GenerationPlanStep>();

    public IList<PlannedArtifact> Artifacts { get; init; } = new List<PlannedArtifact>();
}
