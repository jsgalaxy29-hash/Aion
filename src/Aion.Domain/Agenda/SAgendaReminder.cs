using System;
using Aion.DataEngine.Entities;

namespace Aion.Domain.Agenda;

/// <summary>
/// Rappel déclenché autour d'un événement.
/// </summary>
public class SAgendaReminder : BaseEntity
{
    public int AgendaEventId { get; set; }
    public int OffsetMinutes { get; set; }
    public int ChannelId { get; set; }
    public DateTime TriggerAtUtc { get; set; }
    public bool IsSent { get; set; }
    public DateTime? SentAtUtc { get; set; }

    public virtual SAgendaEvent? AgendaEvent { get; set; }
    public virtual RReminderChannel? Channel { get; set; }
}
