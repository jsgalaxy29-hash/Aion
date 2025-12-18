using System.Collections.Generic;

namespace Aion.AI.Models;

/// <summary>
/// Represents a single embedding vector and the input it was generated from.
/// </summary>
public sealed class EmbeddingVector
{
    public string Input { get; init; } = string.Empty;

    public IReadOnlyList<float> Values { get; init; } = new List<float>();
}
