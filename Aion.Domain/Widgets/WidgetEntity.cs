namespace Aion.Domain.Widgets
{
    /// <summary>
    /// Métadonnées d'un widget disponible dans le catalogue. Chaque widget a un code unique et peut être associé à des données via DataQueryRef.
    /// </summary>
    public sealed class WidgetEntity
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string Code { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string? Component { get; set; }
        public string? ConfigJson { get; set; }
        public bool IsActive { get; set; }
        public string DataQueryRef { get; set; } = default!;
    }
}
