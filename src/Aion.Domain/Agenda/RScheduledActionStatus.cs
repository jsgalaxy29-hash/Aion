using System.Collections.Generic;
using Aion.DataEngine.Entities;

namespace Aion.Domain.Agenda;

/// <summary>
/// Statut d'une action planifiée.
/// </summary>
public class RScheduledActionStatus : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;

    public virtual ICollection<SScheduledAction> ScheduledActions { get; set; } = new List<SScheduledAction>();
}
