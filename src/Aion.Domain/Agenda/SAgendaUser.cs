using Aion.DataEngine.Entities;

namespace Aion.Domain.Agenda;

/// <summary>
/// Décrit les droits d'un utilisateur sur un agenda partagé.
/// </summary>
public class SAgendaUser : BaseEntity
{
    public int AgendaId { get; set; }
    public int UserId { get; set; }
    public bool CanEdit { get; set; }
    public bool CanViewPrivate { get; set; }

    public virtual SAgenda? Agenda { get; set; }
    public virtual SUser? User { get; set; }
}
