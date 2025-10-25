using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Aion.DataEngine.Entities
{
    public class SUser : IdentityUser<Guid>
    {
        // Propriétés de BaseEntity
        public int TenantId { get; set; }
        public bool Actif { get; set; }
        public bool Doc { get; set; }
        public bool Deleted { get; set; }
        public DateTime DtCreation { get; set; }
        public DateTime? DtModification { get; set; }
        public DateTime? DtSuppression { get; set; }
        public int? UsrCreationId { get; set; }
        public int? UsrModificationId { get; set; }
        public int? UsrSuppressionId { get; set; }
        public byte[]? RowVersion { get; set; }

        // Propriétés spécifiques à SUser
        public string Login { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public ClaimsIdentity? Name { get; set; }

        public IList<SGroupUser> Groups { get; set; } = new List<SGroupUser>();

    }
}
