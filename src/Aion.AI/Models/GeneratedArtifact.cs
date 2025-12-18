namespace Aion.AI.Models;

/// <summary>
/// Artifact produced by <see cref="Abstractions.IArtifactGenerator"/>.
/// </summary>
public sealed class GeneratedArtifact
{
    public string ArtifactType { get; init; } = string.Empty;

    public string RelativePath { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;
}
