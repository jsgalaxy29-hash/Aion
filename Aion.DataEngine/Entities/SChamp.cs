using System;
using System.ComponentModel.DataAnnotations;

namespace Aion.DataEngine.Entities
{
    /// <summary>
    /// Represents a field (column) definition stored in the system catalog (S_CHAMP).
    /// Each record defines metadata for a column belonging to a user‑defined table.
    /// </summary>
    public class SChamp
    {
        /// <summary>
        /// Primary key of the S_CHAMP record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the owning table (S_TABLE.Id).
        /// </summary>
        public int TableId { get; set; }

        /// <summary>
        /// Physical name of the column in the database.
        /// </summary>
        [Required]
        public string Libelle { get; set; } = string.Empty;

        /// <summary>
        /// Display alias used in user interfaces.
        /// </summary>
        public string? Alias { get; set; }

        /// <summary>
        /// The SQL data type of the column (e.g. INT, VARCHAR, DATETIME).
        /// </summary>
        [Required]
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// True if the column is part of the primary key.
        /// </summary>
        public bool IsClePrimaire { get; set; }

        /// <summary>
        /// True if the column must be unique.
        /// </summary>
        public bool IsUnique { get; set; }

        /// <summary>
        /// Length or precision of the data type (for types like VARCHAR).
        /// </summary>
        public int Taille { get; set; }

        /// <summary>
        /// Optional reference table name (foreign key target).
        /// </summary>
        public string? Referentiel { get; set; }

        /// <summary>
        /// Optional where clause for filtering the reference table.
        /// </summary>
        public string? ReferentielWhereClause { get; set; }

        /// <summary>
        /// Default value for the column.
        /// </summary>
        public string? Defaut { get; set; }

        /// <summary>
        /// Indicates whether the column is nullable.
        /// </summary>
        public bool IsNulleable { get; set; }

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
        public int? Ordre { get; set; }

        /// <summary>
        /// Display format (e.g. date formats).
        /// </summary>
        public string? Format { get; set; }

        /// <summary>
        /// Input mask (e.g. phone number mask).
        /// </summary>
        public string? Masque { get; set; }

        /// <summary>
        /// True if the column is stored in the database (false for calculated fields).
        /// </summary>
        public bool IsLinkToBdd { get; set; } = true;

        /// <summary>
        /// True if the column is used for searching (GridView filter).
        /// </summary>
        public bool IsSearch { get; set; }

        /// <summary>
        /// Default search operator.
        /// </summary>
        public string? SearchOperator { get; set; }

        /// <summary>
        /// Default search value.
        /// </summary>
        public string? SearchDefautValue { get; set; }

        /// <summary>
        /// X coordinate for UI layout (form design).
        /// </summary>
        public int CoordonneeX { get; set; }

        /// <summary>
        /// Y coordinate for UI layout (form design).
        /// </summary>
        public int CoordonneeY { get; set; }

        /// <summary>
        /// X coordinate of the label for UI layout.
        /// </summary>
        public int CoordonneeLabelX { get; set; }

        /// <summary>
        /// Y coordinate of the label for UI layout.
        /// </summary>
        public int CoordonneeLabelY { get; set; }

        /// <summary>
        /// Optional comment or description.
        /// </summary>
        public string? Commentaire { get; set; }

        /// <summary>
        /// Indicates whether the column holds documents.
        /// </summary>
        public bool Doc { get; set; }

        /// <summary>
        /// Indicates whether the column is active.
        /// </summary>
        public bool Actif { get; set; } = true;

        /// <summary>
        /// Indicates whether the column has been logically deleted.
        /// </summary>
        public bool Deleted { get; set; }

        /// <summary>
        /// Indicates whether changes to this field should be historized.  
        /// If false, the field will be ignored when constructing history entries.
        /// </summary>
        public bool IsHistorise { get; set; } = true;

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        public DateTime DtCreation { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last modification timestamp.
        /// </summary>
        public DateTime DtModification { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Deletion timestamp, if any.
        /// </summary>
        public DateTime? DtSuppression { get; set; }

        /// <summary>
        /// Id of the user who created this definition.
        /// </summary>
        public int UsrCreationId { get; set; }

        /// <summary>
        /// Id of the user who last modified this definition.
        /// </summary>
        public int UsrModificationId { get; set; }

        /// <summary>
        /// Id of the user who deleted this definition.
        /// </summary>
        public int? UsrSuppressionId { get; set; }

        /// <summary>
        /// Navigation property to owning table (populated by EF).
        /// </summary>
        public STable? Table { get; set; }

        /// <summary>
        /// Optional custom script (C# or other language) used for advanced
        /// validation of the value.  When provided, the data engine can
        /// compile and execute this script at runtime to validate the input.
        /// </summary>
        public string? ValidationScript { get; set; }

        /// <summary>
        /// Optional YAML expression used for validation.  This field can
        /// contain rules in a YAML format interpreted by a custom validator.
        /// </summary>
        public string? ValidationYaml { get; set; }
    }
}