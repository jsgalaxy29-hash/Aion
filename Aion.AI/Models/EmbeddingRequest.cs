using System.Collections.Generic;

namespace Aion.AI.Models;

/// <summary>
/// Input describing text to embed and optional parameters.
/// </summary>
public sealed class EmbeddingRequest
{
    public string Model { get; init; } = string.Empty;

    public IList<string> Inputs { get; init; } = new List<string>();

    public int? Dimensions { get; init; }

    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
