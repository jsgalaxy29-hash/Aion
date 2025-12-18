using System.Collections.Generic;
using Aion.DataEngine.Entities;

namespace Aion.Domain.Agenda;

/// <summary>
/// Canal de diffusion d'un rappel (in-app, email, push...).
/// </summary>
public class RReminderChannel : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;

    public virtual ICollection<SAgendaReminder> Reminders { get; set; } = new List<SAgendaReminder>();
}
