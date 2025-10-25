using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Aion.DataEngine.Entities
{
    /// <summary>
    /// Represents a field (column) definition stored in the system catalog.
    /// Each record defines metadata for a column belonging to a user‑defined table.
    /// Les noms de propriétés sont volontairement en anglais pour plus de
    /// cohérence et de lisibilité à travers tout le projet Aion.  Les alias
    /// historiques en français sont conservés via des propriétés marquées
    /// <c>[Obsolete]</c> afin d'assurer la compatibilité avec l'existant.
    /// </summary>
    public class SField : BaseEntity
    {
        /// <summary>
        /// Foreign key to the owning table (S_TABLE.Id).
        /// </summary>
        public int TableId { get; set; }

        /// <summary>
        /// Physical name of the column in the database.
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Obsolete French alias for <see cref="Name"/>.  Use <see cref="Name"/> instead.
        /// </summary>
        [Obsolete("Use Name instead.")]
        public string Libelle
        {
            get => Name;
            set => Name = value;
        }

        /// <summary>
        /// Display alias used in user interfaces.
        /// </summary>
        public string? DisplayLabel { get; set; }

        /// <summary>
        /// Obsolete French alias for <see cref="DisplayLabel"/>.  Use <see cref="DisplayLabel"/> instead.
        /// </summary>
        [Obsolete("Use DisplayLabel instead.")]
        public string? Alias
        {
            get => DisplayLabel;
            set => DisplayLabel = value;
        }

        /// <summary>
        /// The SQL data type of the column (e.g. INT, VARCHAR, DATETIME).
        /// </summary>
        [Required]
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// True if the column is part of the primary key.
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// Obsolete French alias for <see cref="IsPrimaryKey"/>.
        /// </summary>
        [Obsolete("Use IsPrimaryKey instead.")]
        public bool IsClePrimaire
        {
            get => IsPrimaryKey;
            set => IsPrimaryKey = value;
        }

        /// <summary>
        /// True if the column must be unique.
        /// </summary>
        public bool IsUnique { get; set; }

        /// <summary>
        /// Length or precision of the data type (for types like VARCHAR).
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Obsolete French alias for <see cref="Length"/>.
        /// </summary>
        [Obsolete("Use Length instead.")]
        public int Taille
        {
            get => Length;
            set => Length = value;
        }

        /// <summary>
        /// Optional reference table name (foreign key target).
        /// </summary>
        public string? ReferenceTable { get; set; }

        /// <summary>
        /// Obsolete French alias for <see cref="ReferenceTable"/>.
        /// </summary>
        [Obsolete("Use ReferenceTable instead.")]
        public string? Referentiel
        {
            get => ReferenceTable;
            set => ReferenceTable = value;
        }

        /// <summary>
        /// Optional where clause for filtering the reference table.
        /// </summary>
        public string? ReferenceFilter { get; set; }

        /// <summary>
        /// Obsolete French alias for <see cref="ReferenceFilter"/>.
        /// </summary>
        [Obsolete("Use ReferenceFilter instead.")]
        public string? ReferentielWhereClause
        {
            get => ReferenceFilter;
            set => ReferenceFilter = value;
        }

        /// <summary>
        /// Default value for the column.
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Obsolete French alias for <see cref="DefaultValue"/>.
        /// </summary>
        [Obsolete("Use DefaultValue instead.")]
        public string? Defaut
        {
            get => DefaultValue;
            set => DefaultValue = value;
        }

        /// <summary>
        /// Indicates whether the column is nullable.
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Obsolete French alias for <see cref="IsNullable"/>.
        /// </summary>
        [Obsolete("Use IsNullable instead.")]
        public bool IsNulleable
        {
            get => IsNullable;
            set => IsNullable = value;
        }

        /// <summary>
        /// Minimum validation constraint (as a string to allow for numeric or date values).
        /// </summary>
        public string? Min { get; set; }

        /// <summary>
        /// Maximum validation constraint (as a string to allow for numeric or date values).
        /// </summary>
        public string? Max { get; set; }

        /// <summary>
        /// Optional regular expression used to validate input values.
        /// </summary>
        public string? Regex { get; set; }

        /// <summary>
        /// Indicates whether the column should be visible in generated UIs.
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Order of the column in default listings.
        /// </summary>
        public int? Order { get; set; }

        /// <summary>
        /// Obsolete French alias for <see cref="Order"/>.
        /// </summary>
        [Obsolete("Use Order instead.")]
        public int? Ordre
        {
            get => Order;
            set => Order = value;
        }

        /// <summary>
        /// Display format (e.g. date formats).
        /// </summary>
        public string? Format { get; set; }

        /// <summary>
        /// Input mask (e.g. phone number mask).
        /// </summary>
        public string? Mask { get; set; }

        /// <summary>
        /// Obsolete French alias for <see cref="Mask"/>.
        /// </summary>
        [Obsolete("Use Mask instead.")]
        public string? Masque
        {
            get => Mask;
            set => Mask = value;
        }

        /// <summary>
        /// True if the column is stored in the database (false for calculated fields).
        /// </summary>
        public bool IsStored { get; set; } = true;

        /// <summary>
        /// Obsolete French alias for <see cref="IsStored"/>.
        /// </summary>
        [Obsolete("Use IsStored instead.")]
        public bool IsLinkToBdd
        {
            get => IsStored;
            set => IsStored = value;
        }

        /// <summary>
        /// True if the column is used for searching (GridView filter).
        /// </summary>
        public bool IsSearchable { get; set; }

        /// <summary>
        /// Obsolete French alias for <see cref="IsSearchable"/>.
        /// </summary>
        [Obsolete("Use IsSearchable instead.")]
        public bool IsSearch
        {
            get => IsSearchable;
            set => IsSearchable = value;
        }

        /// <summary>
        /// Default search operator.
        /// </summary>
        public string? SearchOperator { get; set; }

        /// <summary>
        /// Default search value.
        /// </summary>
        public string? DefaultSearchValue { get; set; }

        /// <summary>
        /// Obsolete French alias for <see cref="DefaultSearchValue"/>.
        /// </summary>
        [Obsolete("Use DefaultSearchValue instead.")]
        public string? SearchDefautValue
        {
            get => DefaultSearchValue;
            set => DefaultSearchValue = value;
        }

        /// <summary>
        /// X coordinate for UI layout (form design).
        /// </summary>
        public int CoordinateX { get; set; }

        /// <summary>
        /// Obsolete French alias for <see cref="CoordinateX"/>.
        /// </summary>
        [Obsolete("Use CoordinateX instead.")]
        public int CoordonneeX
        {
            get => CoordinateX;
            set => CoordinateX = value;
        }

        /// <summary>
        /// Y coordinate for UI layout (form design).
        /// </summary>
        public int CoordinateY { get; set; }

        /// <summary>
        /// Obsolete French alias for <see cref="CoordinateY"/>.
        /// </summary>
        [Obsolete("Use CoordinateY instead.")]
        public int CoordonneeY
        {
            get => CoordinateY;
            set => CoordinateY = value;
        }

        /// <summary>
        /// X coordinate of the label for UI layout.
        /// </summary>
        public int LabelCoordinateX { get; set; }

        /// <summary>
        /// Obsolete French alias for <see cref="LabelCoordinateX"/>.
        /// </summary>
        [Obsolete("Use LabelCoordinateX instead.")]
        public int CoordonneeLabelX
        {
            get => LabelCoordinateX;
            set => LabelCoordinateX = value;
        }

        /// <summary>
        /// Y coordinate of the label for UI layout.
        /// </summary>
        public int LabelCoordinateY { get; set; }

        /// <summary>
        /// Obsolete French alias for <see cref="LabelCoordinateY"/>.
        /// </summary>
        [Obsolete("Use LabelCoordinateY instead.")]
        public int CoordonneeLabelY
        {
            get => LabelCoordinateY;
            set => LabelCoordinateY = value;
        }

        /// <summary>
        /// Optional comment or description.
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Obsolete French alias for <see cref="Comment"/>.
        /// </summary>
        [Obsolete("Use Comment instead.")]
        public string? Commentaire
        {
            get => Comment;
            set => Comment = value;
        }

        /// <summary>
        /// Indicates whether changes to this field should be historized.  If false,
        /// the field will be ignored when constructing history entries.
        /// </summary>
        public bool IsHistorized { get; set; } = true;

        /// <summary>
        /// Obsolete French alias for <see cref="IsHistorized"/>.
        /// </summary>
        [Obsolete("Use IsHistorized instead.")]
        public bool IsHistorise
        {
            get => IsHistorized;
            set => IsHistorized = value;
        }

        /// <summary>
        /// Navigation property to owning table (populated by EF).
        /// </summary>
        public STable? Table { get; set; }

        /// <summary>
        /// Optional custom script (C# or other language) used for advanced validation of the value.
        /// When provided, the data engine can compile and execute this script at runtime to validate
        /// the input.
        /// </summary>
        public string? ValidationScript { get; set; }

        /// <summary>
        /// Optional YAML expression used for validation.  This field can contain rules in a YAML
        /// format interpreted by a custom validator.
        /// </summary>
        public string? ValidationYaml { get; set; }
    }
}