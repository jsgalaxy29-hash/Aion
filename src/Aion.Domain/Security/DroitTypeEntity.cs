using System;

namespace Aion.Domain.Security
{
    /// <summary>
    /// Décrit un type de droit. Chaque type référence une source d'objets (ex.: S_Menu) et définit le nombre de booléens utilisables.
    /// </summary>
    public sealed class DroitTypeEntity
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        public string Code { get; set; } = default!;
        public string Libelle { get; set; } = default!;
        public string SourceObject { get; set; } = default!;
        public int DroitCount { get; set; } = 5;
        public string Droit1Libelle { get; set; } = "Autorisé";
        public string? Droit2Libelle { get; set; }
        public string? Droit3Libelle { get; set; }
        public string? Droit4Libelle { get; set; }
        public string? Droit5Libelle { get; set; }
    }
}
