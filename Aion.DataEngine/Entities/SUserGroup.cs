namespace Aion.DataEngine.Entities
{
    /// <summary>
    /// Jointure utilisateur ↔ groupe.
    /// Associe un utilisateur à un groupe et indique si le lien est actif.
    /// Règle de fusion : true > false (le droit le plus permissif gagne).
    /// </summary>
    public class SUserGroup : BaseEntity
    {
        /// <summary>
        /// Identifiant de l'utilisateur (FK vers SUser.Id).
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Identifiant du groupe (FK vers SGroup.Id).
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Indique si l'association est active (soft delete du lien).
        /// </summary>
        public bool IsLinkActive { get; set; } = true;

        // Navigation properties
        public virtual SUser? User { get; set; }
        public virtual SGroup? Group { get; set; }
    }
}