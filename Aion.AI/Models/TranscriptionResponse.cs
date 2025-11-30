using System.Collections.Generic;

namespace Aion.AI.Models;

/// <summary>
/// Text and timing returned by a transcription provider.
/// </summary>
public sealed class TranscriptionResponse
{
    public string Text { get; init; } = string.Empty;

    public IList<TranscriptionSegment> Segments { get; init; } = new List<TranscriptionSegment>();

    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
