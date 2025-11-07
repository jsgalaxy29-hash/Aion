using Aion.Domain.Common;

namespace Aion.Domain.AI;

/// <summary>
/// Maps business vocabulary synonyms to canonical terms for NLP.
/// </summary>
public class SXSynonym : BaseEntity
{
    public string DomainTerm { get; set; } = string.Empty;

    public string SynonymsCsv { get; set; } = string.Empty;

    public string Category { get; set; } = "Entity";
}
