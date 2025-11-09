using System.Collections.Generic;
using Aion.DataEngine.Entities;

namespace Aion.Domain.Agenda;

/// <summary>
/// Statut d'un événement d'agenda.
/// </summary>
public class RAgendaEventStatus : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;

    public virtual ICollection<SAgendaEvent> Events { get; set; } = new List<SAgendaEvent>();
}
