using Aion.DataEngine.Entities;
using System;

namespace Aion.DataEngine.Entities
{
    public class SModule : BaseEntity
    {
        public string Name { get; set; } = string.Empty;   // libellé affiché
        public string Description { get; set; } = string.Empty;   // libellé affiché
        public string? Icon { get; set; }
        public int Order { get; set; } = 0;
    }
}
