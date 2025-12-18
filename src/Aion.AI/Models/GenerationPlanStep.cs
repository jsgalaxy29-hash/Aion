using System;
using System.Collections.Generic;

namespace Aion.AI.Models;

/// <summary>
/// A single actionable step in a generation plan.
/// </summary>
public sealed class GenerationPlanStep
{
    public string StepType { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public GenerationStatus Status { get; set; } = GenerationStatus.Draft;

    public IList<string> DependsOn { get; init; } = new List<string>();
}
