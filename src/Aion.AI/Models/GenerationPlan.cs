using System;
using System.Collections.Generic;

namespace Aion.AI.Models;

/// <summary>
/// Represents the orchestration plan that the engine will execute.
/// </summary>
public sealed class GenerationPlan
{
    public Guid PlanId { get; set; } = Guid.NewGuid();

    public string ModuleName { get; init; } = string.Empty;

    public IList<GenerationPlanStep> Steps { get; set; } = new List<GenerationPlanStep>();

    public IList<PlannedArtifact> Artifacts { get; set; } = new List<PlannedArtifact>();
}
