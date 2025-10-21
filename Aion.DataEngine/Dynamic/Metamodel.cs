using System.Collections.Generic;

namespace Aion.DataEngine.Dynamic
{
    /// <summary>
    /// Represents a metamodel containing dynamically defined entities and
    /// fields.  A change in the Version property forces EF Core to
    /// reconstruct the model when used with <see cref="DynamicModelCacheKeyFactory"/>.
    /// </summary>
    public class Metamodel
    {
        /// <summary>
        /// Monotonically increasing version of the metamodel.  When this value
        /// changes, EF Core will rebuild the model to reflect the new
        /// entities and fields.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// The collection of additional fields defined by the user.  These
        /// fields apply to existing or new entities.
        /// </summary>
        public List<AdditionalField> Fields { get; } = new();

        /// <summary>
        /// The collection of additional entities defined by the user.
        /// </summary>
        public List<AdditionalEntity> Entities { get; } = new();
    }
}