using Aion.Domain.Common;

namespace Aion.Domain.AI;

/// <summary>
/// Stores prompt and artifact templates leveraged by the AI orchestrator.
/// </summary>
public class SXTemplate : BaseEntity
{
    public string TemplateKey { get; set; } = string.Empty;

    public string TemplateType { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
