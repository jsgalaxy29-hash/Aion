using System.Collections.Generic;

namespace Aion.Domain.UI.DynamicLayouts
{
    /// <summary>
    /// Représente la configuration personnalisée d'une grille dynamique.
    /// </summary>
    public sealed class DynamicGridLayout
    {
        public int PageSize { get; set; } = 20;
        public List<DynamicGridColumnLayout> Columns { get; set; } = new();
    }

    public sealed class DynamicGridColumnLayout
    {
        public string FieldName { get; set; } = string.Empty;
        public bool Visible { get; set; } = true;
        public int Order { get; set; }
        public double? Width { get; set; }
    }
}
