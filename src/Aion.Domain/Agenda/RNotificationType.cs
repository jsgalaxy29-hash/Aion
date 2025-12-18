using System.Collections.Generic;
using Aion.DataEngine.Entities;

namespace Aion.Domain.Agenda;

/// <summary>
/// Type de notification interne.
/// </summary>
public class RNotificationType : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;

    public virtual ICollection<SNotification> Notifications { get; set; } = new List<SNotification>();
}
