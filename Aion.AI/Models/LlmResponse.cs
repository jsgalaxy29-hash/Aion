using System.Collections.Generic;

namespace Aion.AI.Models;

/// <summary>
/// Output returned by a language model provider.
/// </summary>
public sealed class LlmResponse
{
    public string Content { get; init; } = string.Empty;

    public int PromptTokens { get; init; }

    public int CompletionTokens { get; init; }

    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
