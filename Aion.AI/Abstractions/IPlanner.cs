using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Models;

namespace Aion.AI.Abstractions;

/// <summary>
/// Transforms recognized intents into a structured generation plan.
/// </summary>
public interface IPlanner
{
    Task<GenerationPlan> BuildPlanAsync(IntentRecognitionResult intentResult, CancellationToken ct = default);
}
