using Aion.DataEngine.Entities;
using System;

namespace Aion.DataEngine.Entities
{
    public class SMenu : BaseEntity
    {
        public int ModuleId { get; set; }
        public string Code { get; set; } = string.Empty;   // ex: "DASHBOARD"
        public string Title { get; set; } = string.Empty;
        public string? Path { get; set; }                  // ex: "/dashboard"
        public int Order { get; set; } = 0;
        public int ParentId { get; set; }
        public string? Icon { get; set; }
        public string Route { get; set; } = default!;
        public string? RouteParamsSchemaJson { get; set; }
        public bool IsLeaf { get; set; } = true;
        public string? RequiredPermissionsCsv { get; set; }

    }
}
