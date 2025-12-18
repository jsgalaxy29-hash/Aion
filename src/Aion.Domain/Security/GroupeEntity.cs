using System;

namespace Aion.Domain.Security
{
    /// <summary>
    /// Groupe d'utilisateurs. Les droits sont assignés aux groupes, puis les utilisateurs sont rattachés aux groupes.
    /// </summary>
    public sealed class GroupeEntity
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        public string Code { get; set; } = default!;
        public string Libelle { get; set; } = default!;
    }
}
