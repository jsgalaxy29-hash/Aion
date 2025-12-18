using System.Collections.Generic;

namespace Aion.AI.Models;

/// <summary>
/// Input describing a language model completion request.
/// </summary>
public sealed class LlmRequest
{
    public string Model { get; init; } = string.Empty;

    public IList<LlmMessage> Messages { get; init; } = new List<LlmMessage>();

    public int? MaxOutputTokens { get; init; }

    public float? Temperature { get; init; }

    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
