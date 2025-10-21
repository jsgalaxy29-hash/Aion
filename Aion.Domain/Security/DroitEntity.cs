using System;

namespace Aion.Domain.Security
{
    /// <summary>
    /// Instance de droit assignée à un groupe sur une cible définie par DroitType.SourceObject.
    /// Par exemple, pour le type 'Menu', TargetId correspond à l'Id d'un MenuEntity.
    /// Les booléens Droit1..Droit5 sont interprétés selon le DroitType correspondant.
    /// </summary>
    public sealed class DroitEntity
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        public int GroupeId { get; set; }
        public int DroitTypeId { get; set; }
        public int? TargetId { get; set; }
        public string? TargetKey { get; set; }
        public bool Droit1 { get; set; }
        public bool Droit2 { get; set; }
        public bool Droit3 { get; set; }
        public bool Droit4 { get; set; }
        public bool Droit5 { get; set; }
    }
}
