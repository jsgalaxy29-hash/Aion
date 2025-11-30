using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Models;

namespace Aion.AI.Abstractions;

/// <summary>
/// Abstraction for generating vector embeddings from text or other inputs.
/// </summary>
public interface IEmbeddingProvider
{
    Task<EmbeddingResponse> EmbedAsync(EmbeddingRequest request, CancellationToken ct = default);
}
