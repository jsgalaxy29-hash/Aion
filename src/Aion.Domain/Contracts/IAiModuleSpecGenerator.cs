using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.ModuleBuilder;

namespace Aion.Domain.Contracts;

/// <summary>
/// Abstraction responsible for transforming a natural language prompt into a structured blueprint.
/// </summary>
public interface IAiModuleSpecGenerator
{
    Task<AiModuleBlueprint> GenerateAsync(string naturalLanguagePrompt, CancellationToken cancellationToken = default);
}
