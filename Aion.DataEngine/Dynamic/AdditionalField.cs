using System;

namespace Aion.DataEngine.Dynamic
{
    /// <summary>
    /// Describes a dynamically defined property of an entity.  The
    /// <see cref="PropertyType"/> holds the .NET type of the property.
    /// </summary>
    public class AdditionalField
    {
        /// <summary>
        /// Name of the entity to which this field belongs.
        /// </summary>
        public string EntityName { get; set; } = string.Empty;

        /// <summary>
        /// The property name of the field on the entity.
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// The CLR type of the property (e.g. typeof(string)).
        /// </summary>
        public Type PropertyType { get; set; } = typeof(string);

        /// <summary>
        /// Indicates whether the field is required (NOT NULL).
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Optional maximum length for string properties.
        /// </summary>
        public int? MaxLength { get; set; }
    }
}