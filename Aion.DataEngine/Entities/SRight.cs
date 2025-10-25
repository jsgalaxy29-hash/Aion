
namespace Aion.DataEngine.Entities
{
    /// <summary>
    /// Enregistre les droits attribués à un utilisateur pour un sujet donné.  Le sujet est défini
    /// par son type (<see cref="SRightType"/>) et un identifiant de ressource (menu, table,
    /// module, action ou rapport).  Trois axes (Right1, Right2, Right3) permettent d’exprimer
    /// lecture, écriture, suppression ou d’autres autorisations selon le type.
    /// </summary>
    public class SRight : BaseEntity
    {
        /// <summary>
        /// Identifiant du groupe auquel le droit est accordé.
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Code du type de sujet (correspond au champ Code de <see cref="SRightType"/>).
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// Identifiant de la ressource visée (menu, table, module, action, rapport...).
        /// </summary>
        public int SubjectId { get; set; }

        /// <summary>
        /// Axe 1 (ex: Lecture pour les menus et tables).
        /// </summary>
        public bool? Right1 { get; set; }

        /// <summary>
        /// Axe 2 (ex: Écriture pour les menus et tables).
        /// </summary>
        public bool? Right2 { get; set; }

        /// <summary>
        /// Axe 3 (ex: Suppression pour les tables).
        /// </summary>
        public bool? Right3 { get; set; }
        

    }
}