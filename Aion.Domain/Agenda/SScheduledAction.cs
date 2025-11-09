using System;
using Aion.DataEngine.Entities;

namespace Aion.Domain.Agenda;

/// <summary>
/// Action planifiée à exécuter selon une expression cron.
/// </summary>
public class SScheduledAction : BaseEntity
{
    public int ActionId { get; set; }
    public string Libelle { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public DateTime NextRunUtc { get; set; }
    public DateTime? LastRunUtc { get; set; }
    public int StatusId { get; set; }
    public string? ParametersJson { get; set; }
    public string? LastError { get; set; }

    public virtual SAction? Action { get; set; }
    public virtual RScheduledActionStatus? Status { get; set; }
}
