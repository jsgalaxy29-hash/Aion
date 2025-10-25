using System;
using System.Collections.Generic;

namespace Aion.DataEngine.Entities
{
    /// <summary>
    /// Aggregated view of a history entry used when retrieving history for a
    /// record.  It groups a version and its associated changes into a
    /// single object for ease of consumption by the application layer.
    /// </summary>
    public class SHistorique : BaseEntity
    {
        /// <summary>
        /// Identifier of the history version.
        /// </summary>
        public int VersionId { get; set; }

        /// <summary>
        /// Sequential version number for the record.
        /// </summary>
        public int VersionNum { get; set; }

        /// <summary>
        /// Operation performed (INSERT, UPDATE, DELETE).
        /// </summary>
        public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp of the operation.
        /// </summary>
        public DateTime DtVersion { get; set; }

        /// <summary>
        /// Snapshot of the record's data associated with this version, if
        /// available.
        /// </summary>
        public string? SnapshotJson { get; set; }

        /// <summary>
        /// List of field changes for this version.
        /// </summary>
        public IList<SHistoChange> Changes { get; set; } = new List<SHistoChange>();
        public string TableName { get; internal set; }
        public string RecordId { get; internal set; }
        public string? FieldName { get; internal set; }
        public string? OldValue { get; internal set; }
        public string? NewValue { get; internal set; }
        public int UserId { get; internal set; }
    }
}