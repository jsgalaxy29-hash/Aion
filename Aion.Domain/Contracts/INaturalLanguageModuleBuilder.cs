using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.ModuleBuilder;

namespace Aion.Domain.Contracts;

/// <summary>
/// Coordinates the translation of a validated blueprint into Aion's dynamic metadata tables.
/// </summary>
public interface INaturalLanguageModuleBuilder
{
    Task BuildAsync(AiModuleBlueprint blueprint, CancellationToken cancellationToken = default);
}
