using Aion.DataEngine.Entities;
using System;

namespace Aion.DataEngine.Entities
{
    public class SModule : BaseEntity
    {
        public string Code { get; set; } = string.Empty;   // ex: "CORE", "CRM"
        public string Name { get; set; } = string.Empty;   // libellé affiché
        public int Order { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }
}
