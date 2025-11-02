namespace Aion.DataEngine.Entities
{
    public class SModule : BaseEntity
    {
        public string Name { get; set; } = string.Empty;   // libell affich
        public string Description { get; set; } = string.Empty;   // libell affich
        public string Route { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public int Order { get; set; } = 0;
    }
}
