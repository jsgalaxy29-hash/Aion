using System;
using System.Collections.Generic;

namespace Aion.Domain.Marketplace;

/// <summary>
/// Represents a portable description of a module that can be exchanged via the marketplace.
/// </summary>
public sealed class ModuleTemplate
{
    /// <summary>
    /// Template schema version. Used to guarantee compatibility between exporters and importers.
    /// </summary>
    public string Version { get; set; } = CurrentVersion;

    /// <summary>
    /// Cryptographic signature of the template payload (excluding the signature itself).
    /// </summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the template was generated.
    /// </summary>
    public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;

    public ModuleTemplateDescriptor Module { get; set; } = new();

    public IList<MenuTemplate> Menus { get; set; } = new List<MenuTemplate>();

    public IList<TableTemplate> Tables { get; set; } = new List<TableTemplate>();

    public const string CurrentVersion = "1.0.0";
}

/// <summary>
/// Core module metadata exported/imported by the marketplace.
/// </summary>
public sealed class ModuleTemplateDescriptor
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Route { get; set; } = string.Empty;

    public string? Icon { get; set; }

    public int Order { get; set; }
}

/// <summary>
/// Represents a navigation menu entry linked to the module.
/// </summary>
public sealed class MenuTemplate
{
    public string Label { get; set; } = string.Empty;

    public string? ParentLabel { get; set; }

    public string? Icon { get; set; }

    public bool IsLeaf { get; set; }

    public int Order { get; set; }

    public string? Parameters { get; set; }
}

/// <summary>
/// Describes a logical table belonging to the module.
/// </summary>
public sealed class TableTemplate
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Parent { get; set; }

    public string? ParentLink { get; set; }

    public string? ReferentielLibelle { get; set; }

    public string? Type { get; set; }

    public bool IsHistorise { get; set; }

    public IList<FieldTemplate> Fields { get; set; } = new List<FieldTemplate>();
}

/// <summary>
/// Describes a single field within a table template.
/// </summary>
public sealed class FieldTemplate
{
    public string Name { get; set; } = string.Empty;

    public string? Alias { get; set; }

    public string DataType { get; set; } = string.Empty;

    public bool IsPrimaryKey { get; set; }

    public bool IsUnique { get; set; }

    public bool IsNullable { get; set; }

    public int? Length { get; set; }

    public string? Referentiel { get; set; }

    public string? ReferentielWhereClause { get; set; }

    public string? DefaultValue { get; set; }

    public string? Min { get; set; }

    public string? Max { get; set; }

    public bool IsVisible { get; set; } = true;

    public int? Order { get; set; }

    public string? Format { get; set; }

    public string? Mask { get; set; }

    public bool IsSearch { get; set; }

    public string? SearchOperator { get; set; }

    public string? SearchDefaultValue { get; set; }

    public int CoordinateX { get; set; }

    public int CoordinateY { get; set; }

    public int CoordinateLabelX { get; set; }

    public int CoordinateLabelY { get; set; }

    public bool IsHistorise { get; set; } = true;

    public string? Comment { get; set; }

    public string? ValidationScript { get; set; }

    public string? ValidationYaml { get; set; }
}
