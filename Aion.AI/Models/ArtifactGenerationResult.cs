using System.Collections.Generic;

namespace Aion.AI.Models;

/// <summary>
/// Output of the artifact generator.
/// </summary>
public sealed class ArtifactGenerationResult
{
    public IList<GeneratedArtifact> Artifacts { get; init; } = new List<GeneratedArtifact>();

    public IList<string> Warnings { get; init; } = new List<string>();
}
