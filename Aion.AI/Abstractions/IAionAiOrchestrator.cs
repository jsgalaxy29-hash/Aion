using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Models;

namespace Aion.AI.Abstractions;

/// <summary>
/// Entry point for the AI generation engine.
/// </summary>
public interface IAionAiOrchestrator
{
    Task<GenerationResult> HandleNaturalLanguageRequestAsync(string requestText, CancellationToken ct = default);
}
