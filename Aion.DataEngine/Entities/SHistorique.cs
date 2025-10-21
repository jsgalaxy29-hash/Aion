using System;
using System.Collections.Generic;

namespace Aion.DataEngine.Entities
{
    /// <summary>
    /// Aggregated view of a history entry used when retrieving history for a
    /// record.  It groups a version and its associated changes into a
    /// single object for ease of consumption by the application layer.
    /// </summary>
    public class SHistorique
    {

        /// <summary>
        /// Identifier of the history version.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identifier of the history version.
        /// </summary>
        public int VersionId { get; set; }

        /// <summary>
        /// Sequential version number for the record.
        /// </summary>
        public int Version { get; set; }


        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the name of the database table associated with this instance.
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unique identifier for the record.
        /// </summary>
        public string? RecordId { get; set; }


        /// <summary>
        /// Operation performed (INSERT, UPDATE, DELETE).
        /// </summary>
        public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp of the operation.
        /// </summary>
        public DateTime DtVersion { get; set; }


        /// <summary>
        /// Gets or sets the new value assigned to the property or field during a change event.
        /// </summary>
        public string? NewValue { get; set; }

        /// <summary>
        /// Gets or sets the previous value before a change occurred.
        /// </summary>
        public string? OldValue { get; set; }

        /// <summary>
        /// Gets or sets the name of the field associated with this instance.
        /// </summary>
        public string? FieldName { get; set; }


        /// <summary>
        /// Snapshot of the record's data associated with this version, if
        /// available.
        /// </summary>
        public string? SnapshotJson { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the entity was last modified.
        /// </summary>
        public DateTime DtModification { get; set; }

        /// <summary>
        /// List of field changes for this version.
        /// </summary>
        public IList<SHistoChange> Changes { get; set; } = new List<SHistoChange>();
    }
}