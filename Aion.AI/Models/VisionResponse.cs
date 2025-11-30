using System.Collections.Generic;

namespace Aion.AI.Models;

/// <summary>
/// Output describing the analysis of the provided images.
/// </summary>
public sealed class VisionResponse
{
    public string Content { get; init; } = string.Empty;

    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
