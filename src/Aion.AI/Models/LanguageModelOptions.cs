using System;

namespace Aion.AI.Models;

public sealed class LanguageModelOptions
{
    public string Provider { get; set; } = "Mock";

    public OpenAiOptions OpenAI { get; set; } = new();

    public MistralOptions Mistral { get; set; } = new();
}

public abstract class ProviderOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;

    public int MaxRetries { get; set; } = 3;

    public int RetryDelayMilliseconds { get; set; } = 500;
}

public sealed class OpenAiOptions : ProviderOptions
{
    public string Organization { get; set; } = string.Empty;

    public string DefaultBaseUrl => string.IsNullOrWhiteSpace(BaseUrl) ? "https://api.openai.com" : BaseUrl;

    public string DefaultModel => string.IsNullOrWhiteSpace(Model) ? "gpt-4o-mini" : Model;
}

public sealed class MistralOptions : ProviderOptions
{
    public string DefaultBaseUrl => string.IsNullOrWhiteSpace(BaseUrl) ? "https://api.mistral.ai" : BaseUrl;

    public string DefaultModel => string.IsNullOrWhiteSpace(Model) ? "mistral-small" : Model;
}
