using System.Collections.Generic;

namespace Aion.AI.Models;

/// <summary>
/// Information captured by <see cref="Abstractions.IAuditTrailService"/>.
/// </summary>
public sealed class AuditRecord
{
    public string RequestText { get; set; } = string.Empty;

    public string? IntentsJson { get; set; }

    public string? PlanJson { get; set; }

    public string? PatchYaml { get; set; }

    public string? ArtifactsSummary { get; set; }

    public GenerationStatus Status { get; set; } = GenerationStatus.Draft;

    public string? ErrorMessage { get; set; }

    public string ModelVersion { get; set; } = "mock-gpt";

    public IDictionary<string, string> AdditionalMetadata { get; set; } = new Dictionary<string, string>();
}
