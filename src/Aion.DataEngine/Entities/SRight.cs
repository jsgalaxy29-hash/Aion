namespace Aion.DataEngine.Entities
{
    /// <summary>
    /// Enregistre les droits attribués à un groupe pour un sujet donné.
    /// Le sujet est défini par son type (<see cref="SRightType"/>) et un identifiant de ressource.
    /// Cinq axes (Right1-5) permettent d'exprimer différentes autorisations selon le type.
    /// </summary>
    public class SRight : BaseEntity
    {
        /// <summary>
        /// Identifiant du groupe auquel le droit est accordé.
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Code du type de sujet (correspond au champ Code de <see cref="SRightType"/>).
        /// Exemples: "Menu", "Module", "Table", "Action", "Report"
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// Identifiant de la ressource visée (menu, table, module, action, rapport...).
        /// </summary>
        public int SubjectId { get; set; }

        /// <summary>
        /// Axe 1 - Sémantique définie par SRightType.Right1Name (ex: Lecture/Voir).
        /// </summary>
        public bool? Right1 { get; set; }

        /// <summary>
        /// Axe 2 - Sémantique définie par SRightType.Right2Name (ex: Écriture/Modifier).
        /// </summary>
        public bool? Right2 { get; set; }

        /// <summary>
        /// Axe 3 - Sémantique définie par SRightType.Right3Name (ex: Suppression).
        /// </summary>
        public bool? Right3 { get; set; }

        /// <summary>
        /// Axe 4 - Sémantique définie par SRightType.Right4Name (ex: Exécution/Validation).
        /// </summary>
        public bool? Right4 { get; set; }

        /// <summary>
        /// Axe 5 - Sémantique définie par SRightType.Right5Name (ex: Administration).
        /// </summary>
        public bool? Right5 { get; set; }

        // Navigation properties
        public virtual SGroup? Group { get; set; }
    }
}