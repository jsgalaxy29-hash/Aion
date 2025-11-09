using System;
using System.Collections.Generic;
using Aion.DataEngine.Entities;

namespace Aion.Domain.Agenda;

/// <summary>
/// Agenda logique associé à un utilisateur ou à une équipe.
/// </summary>
public class SAgenda : BaseEntity
{
    public string Libelle { get; set; } = string.Empty;
    public int OwnerUserId { get; set; }
    public bool IsShared { get; set; }
    public string? Color { get; set; }
    public string? TimeZoneId { get; set; }
    public bool IsDefault { get; set; }

    public virtual ICollection<SAgendaEvent> Events { get; set; } = new List<SAgendaEvent>();
    public virtual ICollection<SAgendaUser> SharedUsers { get; set; } = new List<SAgendaUser>();
}
