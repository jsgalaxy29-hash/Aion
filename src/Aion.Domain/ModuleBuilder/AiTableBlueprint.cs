using System.Collections.Generic;
using Aion.DataEngine.Entities;

namespace Aion.Domain.ModuleBuilder;

/// <summary>
/// Describes a table derived from a natural language prompt prior to being created in the dynamic catalog.
/// </summary>
public class AiTableBlueprint : BaseEntity
{
    public int ModuleBlueprintId { get; set; }

    public AiModuleBlueprint? ModuleBlueprint { get; set; }

    public string TechnicalName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public IList<AiFieldBlueprint> Fields { get; set; } = new List<AiFieldBlueprint>();
}
