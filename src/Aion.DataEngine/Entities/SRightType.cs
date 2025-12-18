namespace Aion.DataEngine.Entities
{
    /// <summary>
    /// Définit un type de droit (ex: Menu, Module, Table, Action).
    /// Chaque type spécifie la sémantique des 5 axes de droits via Right1-5Name.
    /// </summary>
    public class SRightType : BaseEntity
    {
        /// <summary>
        /// Code unique identifiant le type (ex: "Menu", "Module", "Table", "Action").
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Libellé du type de droit.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Source de données (table, vue) contenant les sujets de ce type.
        /// Ex: "SMenu", "SModule", "STable"
        /// </summary>
        public string DataSource { get; set; } = string.Empty;

        /// <summary>
        /// Libellé du premier axe de droit (ex: "Lecture", "Voir", "Accéder").
        /// </summary>
        public string Right1Name { get; set; } = string.Empty;

        /// <summary>
        /// Libellé du deuxième axe de droit (ex: "Écriture", "Modifier", "Créer").
        /// </summary>
        public string Right2Name { get; set; } = string.Empty;

        /// <summary>
        /// Libellé du troisième axe de droit (ex: "Suppression", "Supprimer").
        /// </summary>
        public string Right3Name { get; set; } = string.Empty;

        /// <summary>
        /// Libellé du quatrième axe de droit (ex: "Exécution", "Valider", "Exporter").
        /// </summary>
        public string Right4Name { get; set; } = string.Empty;

        /// <summary>
        /// Libellé du cinquième axe de droit (ex: "Administration", "Configuration").
        /// </summary>
        public string Right5Name { get; set; } = string.Empty;

        /// <summary>
        /// Ordre d'affichage dans les interfaces de gestion des droits.
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// Indique si ce type de droit est actif.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}