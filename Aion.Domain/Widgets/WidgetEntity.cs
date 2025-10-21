using System;

namespace Aion.Domain.Widgets
{
    /// <summary>
    /// Métadonnées d'un widget disponible dans le catalogue. Chaque widget a un code unique et peut être associé à des données via DataQueryRef.
    /// </summary>
    public sealed class WidgetEntity
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        public string Code { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string? Icon { get; set; }
        public int DefaultW { get; set; } = 3;
        public int DefaultH { get; set; } = 2;
        public string DataQueryRef { get; set; } = default!;
        public string? SettingsSchemaJson { get; set; }
        public string? RequiredPermissionsCsv { get; set; }
    }
}
