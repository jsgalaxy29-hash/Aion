using System.Collections.Generic;

namespace Aion.AI.Models;

/// <summary>
/// Represents a single intent recognized in the prompt.
/// </summary>
public sealed class DetectedIntent
{
    public RecognizedIntentType Type { get; init; }

    public string? TargetModule { get; init; }

    public IReadOnlyList<string> TargetEntities { get; init; } = new List<string>();

    public IDictionary<string, string> Constraints { get; init; } = new Dictionary<string, string>();
}
