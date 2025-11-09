using System;
using Aion.DataEngine.Entities;

namespace Aion.Domain.Agenda;

/// <summary>
/// Abonnement Web Push pour envoyer des notifications natives.
/// </summary>
public class SPushSubscription : BaseEntity
{
    public int UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public string? DeviceInfo { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastUsedUtc { get; set; }

    public virtual SUser? User { get; set; }
}
