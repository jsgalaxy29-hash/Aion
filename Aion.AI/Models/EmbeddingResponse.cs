using System.Collections.Generic;

namespace Aion.AI.Models;

/// <summary>
/// Embedding vectors returned by a provider.
/// </summary>
public sealed class EmbeddingResponse
{
    public IList<EmbeddingVector> Embeddings { get; init; } = new List<EmbeddingVector>();

    public int PromptTokens { get; init; }

    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
