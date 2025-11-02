namespace Aion.DataEngine.Entities
{
    public class SMenu : BaseEntity
    {
        public int? ModuleId { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public int Order { get; set; } = 0;
        public int? ParentId { get; set; }
        public string? Icon { get; set; }
        public bool IsLeaf { get; set; } = true;

        public SModule? Module { get; set; }
    }
}
