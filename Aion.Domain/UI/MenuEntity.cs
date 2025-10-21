using System;

namespace Aion.Domain.UI
{
    /// <summary>
    /// Entrée de menu hiérarchique. Chaque menu appartient à un module et un tenant.
    /// Les entrées feuilles pointent vers des routes Blazor. Les conteneurs permettent de grouper des feuilles.
    /// </summary>
    public sealed class MenuEntity
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        public int ModuleId { get; set; }
        public int? ParentId { get; set; }
        public string Libelle { get; set; } = default!;
        public string? Icon { get; set; }
        public string Route { get; set; } = default!;
        public string? RouteParamsSchemaJson { get; set; }
        public int Order { get; set; }
        public bool IsLeaf { get; set; } = true;
        public string? RequiredPermissionsCsv { get; set; }
    }
}
