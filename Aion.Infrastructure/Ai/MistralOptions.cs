using System.Diagnostics.CodeAnalysis;

namespace Aion.Infrastructure.Ai;

/// <summary>
/// Configuration for the Mistral chat completion API.
/// </summary>
public class MistralOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.mistral.ai";

    public string Model { get; set; } = "mistral-small-latest";

    public bool IsConfigured([NotNullWhen(true)] out string? reason)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            reason = "Mistral API key is missing.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            reason = "Mistral base URL is missing.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Model))
        {
            reason = "Mistral model is missing.";
            return false;
        }

        reason = null;
        return true;
    }
}
