using System;

namespace Aion.Domain.Security
{
    /// <summary>
    /// Liaison entre un utilisateur et un groupe. Permet de déterminer les droits d'un utilisateur.
    /// La clé primaire est composite (GroupeId, UserId, TenantId).
    /// </summary>
    public sealed class GroupeUserEntity
    {
        public int GroupeId { get; set; }
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
    }
}
