using System;
using System.Collections.Generic;
using Aion.DataEngine.Entities;

namespace Aion.Domain.Agenda;

/// <summary>
/// Événement affiché dans un agenda FullCalendar.
/// </summary>
public class SAgendaEvent : BaseEntity
{
    public int AgendaId { get; set; }
    public string Libelle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public bool AllDay { get; set; }
    public bool IsPrivate { get; set; }
    public int StatusId { get; set; }
    public string? ContextEntityType { get; set; }
    public Guid? ContextEntityId { get; set; }
    public bool EnableReminders { get; set; }

    public virtual SAgenda? Agenda { get; set; }
    public virtual RAgendaEventStatus? Status { get; set; }
    public virtual ICollection<SAgendaReminder> Reminders { get; set; } = new List<SAgendaReminder>();
}
