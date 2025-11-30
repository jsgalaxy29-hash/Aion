namespace Aion.AI.Models;

/// <summary>
/// Represents a single message in a conversational prompt.
/// </summary>
public sealed class LlmMessage
{
    public string Role { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;
}
