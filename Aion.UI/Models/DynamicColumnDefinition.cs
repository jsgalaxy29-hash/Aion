namespace Aion.UI.Models
{
    /// <summary>
    /// Représente une colonne rendue dans la grille dynamique.
    /// Encapsule les métadonnées SField nécessaires à l'affichage
    /// et à l'édition des valeurs.
    /// </summary>
    public sealed class DynamicColumnDefinition
    {
        public DynamicColumnDefinition(SField field)
        {
            Field = field;
            FieldName = field.Libelle;
            DisplayName = string.IsNullOrWhiteSpace(field.Alias) ? field.Libelle : field.Alias!;
            IsPrimaryKey = field.IsClePrimaire;
            IsNullable = field.IsNulleable;
            IsSearchable = field.IsSearch;
            SearchOperator = field.SearchOperator;
            DefaultSearchValue = field.SearchDefautValue;
            Order = field.Ordre ?? int.MaxValue;
            IsReference = !string.IsNullOrWhiteSpace(field.Referentiel);
            InputKind = IsReference ? DynamicColumnInputKind.Select : ResolveInputKind(field.DataType);
            Visible = field.IsVisible;
        }

        public SField Field { get; }
        public string FieldName { get; }
        public string DisplayName { get; }
        public string DataType => Field.DataType;
        public bool Visible { get; set; } = true;
        public int Order { get; set; }
        public bool IsPrimaryKey { get; }
        public bool IsNullable { get; }
        public bool IsSearchable { get; }
        public string? SearchOperator { get; }
        public string? DefaultSearchValue { get; }
        public bool IsReference { get; }
        public DynamicColumnInputKind InputKind { get; }
        public List<DynamicOption>? Options { get; set; }

        private static DynamicColumnInputKind ResolveInputKind(string dataType)
        {
            if (string.IsNullOrWhiteSpace(dataType))
            {
                return DynamicColumnInputKind.Text;
            }

            var type = dataType.Trim().ToLowerInvariant();
            if (type.Contains("char") || type.Contains("text") || type.Contains("xml"))
            {
                return DynamicColumnInputKind.Text;
            }
            if (type.Contains("date"))
            {
                return type.Contains("time") ? DynamicColumnInputKind.DateTime : DynamicColumnInputKind.Date;
            }
            if (type.Contains("time"))
            {
                return DynamicColumnInputKind.DateTime;
            }
            if (type.Contains("bit"))
            {
                return DynamicColumnInputKind.Boolean;
            }
            if (type.Contains("decimal") || type.Contains("numeric") || type.Contains("money"))
            {
                return DynamicColumnInputKind.Decimal;
            }
            if (type.Contains("float") || type.Contains("real"))
            {
                return DynamicColumnInputKind.Decimal;
            }
            if (type.Contains("int"))
            {
                return DynamicColumnInputKind.Number;
            }
            if (type.Contains("uniqueidentifier") || type.Contains("guid"))
            {
                return DynamicColumnInputKind.Text;
            }

            return DynamicColumnInputKind.Text;
        }
    }

    public enum DynamicColumnInputKind
    {
        Text,
        Number,
        Decimal,
        Date,
        DateTime,
        Boolean,
        Select
    }
}
