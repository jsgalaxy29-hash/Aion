using Aion.DataEngine.Entities;

namespace Aion.Domain.ModuleBuilder;

/// <summary>
/// Represents a field definition detected by the AI parsing layer for a target table.
/// </summary>
public class AiFieldBlueprint : BaseEntity
{
    public int TableBlueprintId { get; set; }

    public AiTableBlueprint? TableBlueprint { get; set; }

    public string TechnicalName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string DataType { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public bool IsPrimaryKey { get; set; }

    public bool IsUnique { get; set; }

    public int? MaxLength { get; set; }

    public string? ForeignKeyTargetTable { get; set; }

    public string? ForeignKeyTargetField { get; set; }
}
