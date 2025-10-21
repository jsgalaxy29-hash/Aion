using System;

namespace Aion.Domain.UI
{
    /// <summary>
    /// Représente un module fonctionnel (par ex. CRM, Finance) avec un ordre et un libellé.
    /// Chaque module est associé à un tenant pour le multi‑tenant.
    /// </summary>
    public sealed class ModuleEntity
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        public string Code { get; set; } = default!;
        public string Libelle { get; set; } = default!;
        public string? Icon { get; set; }
        public int Order { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}
