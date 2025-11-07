using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Models;

namespace Aion.AI.Abstractions;

/// <summary>
/// Builds a YAML patch against AION_ROADMAP.yaml that reflects the generation plan.
/// </summary>
public interface IRoadmapPatcher
{
    Task<string> GeneratePatchAsync(string requestText, IntentRecognitionResult intents, GenerationPlan plan, CancellationToken ct = default);
}
