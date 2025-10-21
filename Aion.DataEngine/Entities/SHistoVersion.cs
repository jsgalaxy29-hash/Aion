using System;
using System.Collections.Generic;

namespace Aion.DataEngine.Entities
{
    /// <summary>
    /// Represents a version entry in the history.  Each version corresponds to a
    /// single operation (insert, update, delete) on a specific row of a table.
    /// It stores the target table name, the primary key value of the row, the
    /// version number, the operation type, an optional user id, the timestamp
    /// and an optional snapshot of the record as JSON.  A collection of
    /// <see cref="SHistoChange"/> records describes the changes field by field.
    /// </summary>
    public class SHistoVersion
    {
        /// <summary>
        /// Identity of the version record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The physical table name on which the operation was performed.
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// The primary key value of the row being versioned.  In case of
        /// composite keys this value can contain a concatenation or JSON
        /// representation of the composite key.  It is stored as a string for
        /// simplicity.
        /// </summary>
        public string RowPk { get; set; } = string.Empty;

        /// <summary>
        /// A sequential number for this row.  Version numbers start at 1
        /// and increment by one for each operation on the same row.
        /// </summary>
        public int VersionNum { get; set; }

        /// <summary>
        /// The operation performed: "INSERT", "UPDATE" or "DELETE".
        /// </summary>
        public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// Identifier of the user who performed the operation, if available.
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Timestamp of the operation.
        /// </summary>
        public DateTime DtVersion { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional JSON snapshot of the record's data after the operation
        /// (for inserts/updates) or before the operation (for deletes).  This
        /// snapshot can be used for quick restoration of the record.
        /// </summary>
        public string? SnapshotJson { get; set; }

        /// <summary>
        /// Optional free text comment attached to the version entry.
        /// </summary>
        public string? Commentaire { get; set; }

        /// <summary>
        /// Collection of changes associated with this version.  Populated when
        /// retrieving versions from the history service.
        /// </summary>
        public IList<SHistoChange> Changes { get; set; } = new List<SHistoChange>();
    }
}