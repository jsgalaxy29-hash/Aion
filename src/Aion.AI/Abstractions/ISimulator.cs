using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Models;

namespace Aion.AI.Abstractions;

/// <summary>
/// Validates a plan and patch without modifying the live system.
/// </summary>
public interface ISimulator
{
    Task<SimulationResult> RunAsync(GenerationPlan plan, string patchYaml, CancellationToken ct = default);
}
