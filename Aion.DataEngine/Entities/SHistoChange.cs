using System;

namespace Aion.DataEngine.Entities
{
    /// <summary>
    /// Represents a single field change recorded in a history version.  Each
    /// instance stores the name of the field (column) and the old and new
    /// values.  Values are stored as strings to simplify persistence and
    /// retrieval.  The parent version is indicated by the foreign key
    /// <see cref="HistoVersionId"/>.
    /// </summary>
    public class SHistoChange
    {
        /// <summary>
        /// Identity of the change record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the associated history version record.
        /// </summary>
        public int HistoVersionId { get; set; }

        /// <summary>
        /// The name of the field that changed.
        /// </summary>
        public string Champ { get; set; } = string.Empty;

        /// <summary>
        /// The old value of the field, serialized as a string.  Null if the
        /// field did not exist before the operation (insert) or if unknown.
        /// </summary>
        public string? OldValue { get; set; }

        /// <summary>
        /// The new value of the field, serialized as a string.  Null if the
        /// field was removed (delete) or if unknown.
        /// </summary>
        public string? NewValue { get; set; }

        /// <summary>
        /// Navigation property back to the version.  This may be populated
        /// when retrieving history from the database.
        /// </summary>
        public SHistoVersion? Version { get; set; }
    }
}