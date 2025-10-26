using System.Collections.Generic;
using System.Threading.Tasks;
using Aion.DataEngine.Entities;

namespace Aion.DataEngine.Interfaces
{
    /// <summary>
    /// Provides runtime validation for values written to user‑defined tables.
    /// Rules are derived from metadata stored in S_CHAMP, such as regular
    /// expressions, ranges and nullability.
    /// </summary>
    public interface IValidationService
    {
        /// <summary>
        /// Validates a record before insertion or update.  Throws an exception if
        /// validation fails.
        /// </summary>
        /// <param name="table">The table definition.</param>
        /// <param name="fields">The collection of field definitions.</param>
        /// <param name="values">The values to validate.</param>
        Task ValidateAsync(STable table, IEnumerable<SField> fields, IDictionary<string, object?> values);
    }
}