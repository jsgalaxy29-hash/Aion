using System.Collections.Generic;
using Aion.DataEngine.Entities;

namespace Aion.Domain.ModuleBuilder;

/// <summary>
/// Represents a user provided prompt and the structured specification generated from it.
/// The blueprint is persisted before being applied to the dynamic catalog tables so that
/// administrators can audit and revisit prior generations.
/// </summary>
public class AiModuleBlueprint : BaseEntity
{
    /// <summary>
    /// Functional name of the module to create.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description shown to administrators.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Natural language prompt provided by the administrator.
    /// </summary>
    public string NaturalLanguagePrompt { get; set; } = string.Empty;

    /// <summary>
    /// JSON document produced by the AI provider that captures the parsed specification.
    /// </summary>
    public string ParsedSpecificationJson { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the blueprint lifecycle.
    /// </summary>
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// Collection of tables discovered from the prompt.
    /// </summary>
    public IList<AiTableBlueprint> Tables { get; set; } = new List<AiTableBlueprint>();
}
