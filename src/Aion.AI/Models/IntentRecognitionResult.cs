using System.Collections.Generic;

namespace Aion.AI.Models;

/// <summary>
/// Result produced by <see cref="Abstractions.IIntentRecognizer"/>.
/// </summary>
public sealed class IntentRecognitionResult
{
    public IReadOnlyList<DetectedIntent> Intents { get; init; } = new List<DetectedIntent>();

    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
