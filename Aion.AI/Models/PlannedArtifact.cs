namespace Aion.AI.Models;

/// <summary>
/// Artifact expected to be produced by the generation pipeline.
/// </summary>
public sealed class PlannedArtifact
{
    public string ArtifactType { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? TargetPath { get; init; }
}
