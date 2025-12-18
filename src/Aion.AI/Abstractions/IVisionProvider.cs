using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Models;

namespace Aion.AI.Abstractions;

/// <summary>
/// Abstraction for AI vision analysis providers.
/// </summary>
public interface IVisionProvider
{
    Task<VisionResponse> AnalyzeAsync(VisionRequest request, CancellationToken ct = default);
}
