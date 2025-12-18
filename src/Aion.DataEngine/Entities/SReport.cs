namespace Aion.DataEngine.Entities
{
    public class SReport : BaseEntity
    {
        /// <summary>
        /// Nom du rapport.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Description du rapport.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// Chemin d’accès au fichier du rapport.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;
    }
}