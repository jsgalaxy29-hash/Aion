using Aion.DataEngine.Entities;

namespace Aion.Domain.AI;

/// <summary>
/// Stores the full trace of an AI driven generation attempt.
/// </summary>
public class SXGenerationLog : BaseEntity
{
    public string RequestText { get; set; } = string.Empty;

    public string? IntentsJson { get; set; }

    public string? PlanJson { get; set; }

    public string? PatchYaml { get; set; }

    public string? ArtifactsSummary { get; set; }

    public string Status { get; set; } = "Draft";

    public string? ErrorMessage { get; set; }

    public string ModelVersion { get; set; } = "unknown";
}
