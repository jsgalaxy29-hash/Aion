using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Models;

namespace Aion.AI.Abstractions;

/// <summary>
/// Generates code artifacts from a plan and roadmap patch.
/// </summary>
public interface IArtifactGenerator
{
    Task<ArtifactGenerationResult> GenerateAsync(GenerationPlan plan, string patchYaml, SimulationResult simulation, CancellationToken ct = default);
}
