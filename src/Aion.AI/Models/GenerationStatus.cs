namespace Aion.AI.Models;

/// <summary>
/// Status of an AI driven generation session.
/// Mirrors the values persisted in <see cref="Aion.Domain.AI.SAuditRecord"/>.
/// </summary>
public enum GenerationStatus
{
    Draft,
    Simulated,
    Applied,
    Failed
}
