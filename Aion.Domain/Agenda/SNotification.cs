using System;
using Aion.DataEngine.Entities;

namespace Aion.Domain.Agenda;

/// <summary>
/// Notification interne affichée dans le centre Aion.
/// </summary>
public class SNotification : BaseEntity
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime? ReadUtc { get; set; }
    public int? NotificationTypeId { get; set; }
    public string? LinkUrl { get; set; }

    public virtual SUser? User { get; set; }
    public virtual RNotificationType? NotificationType { get; set; }
}
