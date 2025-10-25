using System.ComponentModel.DataAnnotations;

namespace Aion.DataEngine.Entities
{
    /// <summary>
    /// Définit un groupe d’utilisateurs.  Les groupes correspondent aux rôles
    /// dans le moteur de droits Aion.  Un utilisateur peut appartenir à
    /// plusieurs groupes, et un groupe peut avoir plusieurs utilisateurs.
    /// </summary>
    public class SGroup : BaseEntity
    {
        /// <summary>
        /// Nom du groupe.  Doit être unique.
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;
    }
}