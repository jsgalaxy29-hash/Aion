using Aion.DataEngine.Entities;
using System;

namespace Aion.DataEngine.Entities
{
    public class SWidget: BaseEntity
    {
        public string Code { get; set; } = string.Empty;   // ex: "KPI_USERS"
        public string Title { get; set; } = string.Empty;
        public string? Component { get; set; }             // nom du composant Blazor
        public string? ConfigJson { get; set; }            // config sérialisée
        public bool IsActive { get; set; } = true;
        public string DataQueryRef { get; set; } = string .Empty; // ex: "ef:LatestModules"
    }
}
