using System.Collections.Generic;

namespace Aion.AI.Models;

/// <summary>
/// Information captured by <see cref="Abstractions.IAuditTrailService"/>.
/// </summary>
public sealed class AuditRecord
{
    public string RequestText { get; init; } = string.Empty;

    public string? IntentsJson { get; init; }

    public string? PlanJson { get; init; }

    public string? PatchYaml { get; init; }

    public string? ArtifactsSummary { get; init; }

    public GenerationStatus Status { get; init; } = GenerationStatus.Draft;

    public string? ErrorMessage { get; init; }

    public string ModelVersion { get; init; } = "mock-gpt";

    public IDictionary<string, string> AdditionalMetadata { get; init; } = new Dictionary<string, string>();
}
