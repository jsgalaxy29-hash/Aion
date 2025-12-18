namespace Aion.AI.Models;

/// <summary>
/// Represents a time-bounded transcription segment.
/// </summary>
public sealed class TranscriptionSegment
{
    public double StartSeconds { get; init; }

    public double EndSeconds { get; init; }

    public string Text { get; init; } = string.Empty;
}
