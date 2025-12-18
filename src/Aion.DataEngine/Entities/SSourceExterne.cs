using System;

namespace Aion.DataEngine.Entities
{
    /// <summary>
    /// Represents an external data source configuration.  Each record
    /// describes a source that can be used for lookups, import or export
    /// operations.  The type determines which provider handles this source
    /// (REST_API, CSV, EXCEL, S3, GED, etc.).  Additional parameters such
    /// as authentication and custom parameters are stored as JSON strings.
    /// </summary>
    public class SSourceExterne : BaseEntity
    {

        /// <summary>
        /// Human‑readable name of the source (e.g. "API Entreprise").
        /// </summary>
        public string Nom { get; set; } = string.Empty;

        /// <summary>
        /// Type of the source.  Supported values include 'REST_API', 'CSV',
        /// 'EXCEL', 'S3', 'GED', etc.  The type determines the provider
        /// implementation to use.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Base URL or file location of the source.  For API sources this
        /// might be the endpoint; for S3 it could be the bucket/prefix; for
        /// CSV/EXCEL it is the file path or UNC.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Authentication type for the source.  Values include 'NONE',
        /// 'API_KEY', 'BASIC', 'BEARER', etc.
        /// </summary>
        public string? AuthType { get; set; }

        /// <summary>
        /// JSON containing authentication parameters (e.g. { "apiKey":
        /// "...", "user": "...", "pass": "...", "token": "..." }).
        /// </summary>
        public string? AuthParams { get; set; }

        /// <summary>
        /// JSON containing provider‑specific parameters (e.g. delimiter,
        /// header presence for CSV; sheet name for Excel; region and bucket
        /// for S3).  The shape depends on the provider type.
        /// </summary>
        public string? Params { get; set; }

    }
}