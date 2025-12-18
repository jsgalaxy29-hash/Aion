using System;
using System.Collections.Generic;

namespace Aion.AI.Models;

/// <summary>
/// Input describing an audio transcription request.
/// </summary>
public sealed class TranscriptionRequest
{
    public string FileName { get; init; } = string.Empty;

    public ReadOnlyMemory<byte> Audio { get; init; }

    public string? Prompt { get; init; }

    public string? Language { get; init; }

    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
