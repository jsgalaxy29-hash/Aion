using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Aion.DataEngine.Entities
{
    /// <summary>
    /// Represents a logical table definition stored in the system catalog (S_TABLE).
    /// Each record defines a user‑defined table that can be materialized in the database.
    /// </summary>
    public class STable
    {
        /// <summary>
        /// Primary key of the S_TABLE record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Logical name of the table (must correspond to the physical table when materialized).
        /// </summary>
        [Required]
        public string Libelle { get; set; } = string.Empty;

        /// <summary>
        /// Optional description for documentation purposes.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Optional name of the parent table for hierarchical relationships.  
        /// This value is used by the search panel to implement cross‑table lookups.
        /// </summary>
        public string? Parent { get; set; }

        /// <summary>
        /// Name of the column linking the child table to its parent.
        /// </summary>
        public string? ParentLiaison { get; set; }

        /// <summary>
        /// Name of the display field on the parent table used when referencing this table.
        /// </summary>
        public string? ReferentielLibelle { get; set; }

        /// <summary>
        /// Type of table.  F: Form; R: Reference; S: System; V: View.
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Indicates whether the table holds documents.
        /// </summary>
        public bool Doc { get; set; }

        /// <summary>
        /// Indicates whether the table is active.
        /// </summary>
        public bool Actif { get; set; } = true;

        /// <summary>
        /// Indicates whether the table has been logically deleted.
        /// </summary>
        public bool Deleted { get; set; }

        /// <summary>
        /// Indicates whether changes on this table should be historized.  
        /// When set to false, the data engine will skip writing entries to the
        /// history service for operations on this table.  Default is true.
        /// </summary>
        public bool IsHistorise { get; set; } = true;

        /// <summary>
        /// Date of creation.
        /// </summary>
        public DateTime DtCreation { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date of last modification.
        /// </summary>
        public DateTime DtModification { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date of deletion, if any.
        /// </summary>
        public DateTime? DtSuppression { get; set; }

        /// <summary>
        /// Identifier of the user who created this definition.
        /// </summary>
        public int UsrCreationId { get; set; }

        /// <summary>
        /// Identifier of the user who last modified this definition.
        /// </summary>
        public int UsrModificationId { get; set; }

        /// <summary>
        /// Identifier of the user who deleted this definition.
        /// </summary>
        public int? UsrSuppressionId { get; set; }

        /// <summary>
        /// The collection of fields (columns) associated with this table.
        /// </summary>
        public IList<SChamp> Champs { get; set; } = new List<SChamp>();
    }
}