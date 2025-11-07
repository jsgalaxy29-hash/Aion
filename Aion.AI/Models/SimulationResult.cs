using System.Collections.Generic;

namespace Aion.AI.Models;

/// <summary>
/// Result of a dry-run performed by <see cref="Abstractions.ISimulator"/>.
/// </summary>
public sealed class SimulationResult
{
    public bool IsSuccessful { get; set; }

    public IList<string> Warnings { get; init; } = new List<string>();

    public IList<string> Errors { get; init; } = new List<string>();
}
