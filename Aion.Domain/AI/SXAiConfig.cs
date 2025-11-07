using Aion.Domain.Common;

namespace Aion.Domain.AI;

/// <summary>
/// Configures the behaviour of the AI generation engine.
/// </summary>
public class SXAiConfig : BaseEntity
{
    public string Provider { get; set; } = "Mock";

    public string? ApiKey { get; set; }

    public string? BaseUrl { get; set; }

    public string ModelName { get; set; } = "mock-gpt";

    public double Temperature { get; set; } = 0.1d;

    public int MaxTokens { get; set; } = 2048;

    public bool IsEnabled { get; set; } = true;
}
