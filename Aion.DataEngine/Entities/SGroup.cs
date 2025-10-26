using System.ComponentModel.DataAnnotations;

namespace Aion.DataEngine.Entities
{
    /// <summary>
    /// Représente un groupe d'utilisateurs.
    /// Les droits sont attribués au niveau du groupe via <see cref="SRight"/>.
    /// </summary>
    public class SGroup : BaseEntity
    {
        /// <summary>
        /// Nom unique du groupe.
        /// </summary>
        [Required, MaxLength(128)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description du groupe.
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Indique si le groupe est un groupe système (non modifiable).
        /// </summary>
        public bool IsSystem { get; set; } = false;

        // Navigation properties
        public virtual ICollection<SUserGroup> UserGroups { get; set; } = new List<SUserGroup>();
        public virtual ICollection<SRight> Rights { get; set; } = new List<SRight>();
    }
}