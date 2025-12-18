using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aion.Infrastructure.Ai;

internal sealed class MistralChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public IList<MistralChatMessage> Messages { get; set; } = new List<MistralChatMessage>();

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }
}

internal sealed class MistralChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public object? Content { get; set; }
}

internal sealed class MistralChatResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("choices")]
    public IList<MistralChatChoice>? Choices { get; set; }
}

internal sealed class MistralChatChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public MistralChatMessageContent? Message { get; set; }
}

internal sealed class MistralChatMessageContent
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public JsonElement Content { get; set; }

    public string GetTextContent()
    {
        if (Content.ValueKind == JsonValueKind.String)
        {
            return Content.GetString() ?? string.Empty;
        }

        if (Content.ValueKind == JsonValueKind.Array)
        {
            var parts = Content.EnumerateArray()
                .Select(ExtractContentText)
                .Where(part => !string.IsNullOrWhiteSpace(part));
            return string.Join("\n", parts);
        }

        if (Content.ValueKind == JsonValueKind.Object)
        {
            var text = ExtractContentText(Content);
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return Content.ToString() ?? string.Empty;
    }

    private static string ExtractContentText(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString() ?? string.Empty;
        }

        if (element.TryGetProperty("text", out var textProperty))
        {
            return textProperty.GetString() ?? string.Empty;
        }

        return element.ToString();
    }
}
