using System.ComponentModel.DataAnnotations;

namespace Aion.DataEngine.Entities
{
    /// <summary>
    /// Represents a reusable regular expression definition.  Instances of this
    /// entity are stored in the reference table R_REGEX.  The `Pattern`
    /// property contains the actual regular expression and may be selected
    /// from a dropdown when defining a field in S_CHAMP.
    /// </summary>
    public class RRegex
    {
        /// <summary>
        /// Primary key of the regex definition.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Unique name identifying the regex pattern.
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The regular expression pattern.
        /// </summary>
        [Required]
        public string Pattern { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of the pattern for documentation or UI.
        /// </summary>
        public string? Description { get; set; }
    }
}