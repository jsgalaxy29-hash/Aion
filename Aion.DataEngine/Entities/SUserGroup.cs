
namespace Aion.DataEngine.Entities
{
    /// <summary>
    /// Jointure utilisateur ↔ groupe.  Chaque enregistrement associe un
    /// utilisateur à un groupe et indique si le lien est actif.  Tous les
    /// champs système sont hérités de <see cref="BaseEntity"/>.
    /// </summary>
    public class SUserGroup : BaseEntity
    {
        /// <summary>
        /// Identifiant de l’utilisateur.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Identifiant du groupe.
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Indique si l’association est active (soft delete du lien).
        /// </summary>
        public bool IsLinkActive { get; set; } = true;

    }
}