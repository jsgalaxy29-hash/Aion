using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.Marketplace;

namespace Aion.Domain.Contracts;

/// <summary>
/// Handles import/export of module templates used by the marketplace.
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// Exports the specified module into a portable template.
    /// </summary>
    Task<ModuleTemplate> ExportModuleAsync(int moduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a module template into the current tenant and returns the identifier of the created module.
    /// </summary>
    Task<int> ImportModuleAsync(ModuleTemplate template, CancellationToken cancellationToken = default);
}
