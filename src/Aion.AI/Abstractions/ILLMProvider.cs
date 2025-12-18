using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Models;

namespace Aion.AI.Abstractions;

/// <summary>
/// Abstraction for large language model providers.
/// </summary>
public interface ILLMProvider
{
    Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default);
}
