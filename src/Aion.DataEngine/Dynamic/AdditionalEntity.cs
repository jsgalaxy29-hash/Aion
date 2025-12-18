using System.Collections.Generic;

namespace Aion.DataEngine.Dynamic
{
    /// <summary>
    /// Describes a dynamically defined entity (table) including its key and
    /// additional fields.  The <see cref="EntityName"/> is the CLR type name
    /// used by EF Core while <see cref="TableName"/> is the physical table name.
    /// </summary>
    public class AdditionalEntity
    {
        /// <summary>
        /// CLR type name of the entity.  Used by EF Core as entity identifier.
        /// </summary>
        public string EntityName { get; set; } = string.Empty;

        /// <summary>
        /// Name of the table in the database.
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Optional schema name for the table (default schema if null).
        /// </summary>
        public string? TableSchema { get; set; }

        /// <summary>
        /// List of key fields defining the primary key of the entity.
        /// </summary>
        public List<AdditionalField> Key { get; } = new();

        /// <summary>
        /// List of additional fields on the entity.
        /// </summary>
        public List<AdditionalField> Fields { get; } = new();
    }
}