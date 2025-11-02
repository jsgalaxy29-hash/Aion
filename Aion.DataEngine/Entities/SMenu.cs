using Aion.DataEngine.Entities;
using System;

namespace Aion.DataEngine.Entities
{
    public class SMenu : BaseEntity
    {
        public int ModuleId { get; set; } = 0;
        public string Libelle { get; set; } = string.Empty;
        public int Order { get; set; } = 0;
        public int? ParentId { get; set; }
        public string? Icon { get; set; }
        public string Route { get; set; } = default!;
        public bool IsLeaf { get; set; } = true;
    }
}
