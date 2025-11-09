using System;

namespace Aion.DataEngine.Entities
{
    /// <summary>
    /// Represents the binding between an external source and a table/column in the
    /// dynamic database.  Each binding tells the engine how to read or write
    /// data from a source for a given table and (optionally) column.
    /// </summary>
    public class SSourceBinding : BaseEntity
    {
       
        /// <summary>
        /// Foreign key to the external source configuration.
        /// </summary>
        public int SourceId { get; set; }

        /// <summary>
        /// Foreign key to the logical table (S_TABLE) this binding applies to.
        /// </summary>
        public int TableId { get; set; }

        /// <summary>
        /// Foreign key to the logical column (SFIELD) this binding applies to.
        /// Null if the binding operates at table level (e.g. import/export).
        /// </summary>
        public int? ChampId { get; set; }

        /// <summary>
        /// Extraction path used by the provider to locate the data within
        /// the source.  For JSON data this could be a JSONPath; for CSV or
        /// Excel it is the column name; for S3/GED it might be a folder or
        /// key prefix.
        /// </summary>
        public string? ExtractionPath { get; set; }

        /// <summary>
        /// Mode of the binding: 'READ' for lookups, 'WRITE' for export,
        /// 'SYNC' for bidirectional synchronization.  Future modes can be
        /// added as needed.
        /// </summary>
        public string Mode { get; set; } = string.Empty;

        /// <summary>
        /// JSON describing how fields from the source map to fields in the
        /// dynamic table.  Example: { "apiField": "siret", "localField": "SIRET" }.
        /// </summary>
        public string? MappingJson { get; set; }
    }
}