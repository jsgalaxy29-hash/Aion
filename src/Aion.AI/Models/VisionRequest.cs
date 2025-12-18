using System.Collections.Generic;

namespace Aion.AI.Models;

/// <summary>
/// Input describing a multi-modal prompt for a vision model.
/// </summary>
public sealed class VisionRequest
{
    public string Model { get; init; } = string.Empty;

    public string Prompt { get; init; } = string.Empty;

    public IList<VisionImage> Images { get; init; } = new List<VisionImage>();

    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
