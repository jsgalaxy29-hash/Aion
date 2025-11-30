using System;

namespace Aion.AI.Models;

/// <summary>
/// Represents an image to analyze with optional description.
/// </summary>
public sealed class VisionImage
{
    public string ContentType { get; init; } = string.Empty;

    public ReadOnlyMemory<byte> Data { get; init; }

    public string? Caption { get; init; }
}
