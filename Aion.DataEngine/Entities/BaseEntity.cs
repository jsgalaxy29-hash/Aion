using System;
using System.ComponentModel.DataAnnotations;

namespace Aion.DataEngine.Entities
{
    /// <summary>Common system fields. All Aion entities inherit this base.</summary>
    public abstract class BaseEntity
    {


        public int Id { get; set; }
        /// <summary>
        /// Identifiant du locataire (tenant). 1 = DefaultTenant.
        /// </summary>
        public int TenantId { get; set; } = 1;
        public bool Actif { get; set; } = true;
        public bool Doc { get; set; } = false;
        public bool Deleted { get; set; } = false;

        public DateTime DtCreation { get; set; }
        public DateTime? DtModification { get; set; }
        public DateTime? DtSuppression { get; set; }

        public int? UsrCreationId { get; set; }
        public int? UsrModificationId { get; set; }
        public int? UsrSuppressionId { get; set; }

        [Timestamp] public byte[]? RowVersion { get; set; }
    }
}
