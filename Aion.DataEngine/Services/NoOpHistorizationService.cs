using System.Collections.Generic;
using System.Threading.Tasks;
using Aion.DataEngine.Interfaces;

namespace Aion.DataEngine.Services
{
    /// <summary>
    /// Dummy implementation of <see cref="IHistorizationService"/> that does nothing.
    /// This placeholder can be replaced with a proper implementation writing
    /// history entries into a table (e.g. S_HISTORIQUE).
    /// </summary>
    public class NoOpHistorizationService : IHistorizationService
    {
        /// <inheritdoc />
        public Task SaveHistoryAsync(string tableName, string operation, object primaryKey, IDictionary<string, object?> newValues, IDictionary<string, object?>? oldValues = null)
        {
            // No history is recorded in this implementation.
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IList<Entities.SHistorique>> GetHistoryAsync(string tableName, object primaryKey)
        {
            // Return an empty history.
            return Task.FromResult<IList<Entities.SHistorique>>(new List<Entities.SHistorique>());
        }

        /// <inheritdoc />
        public Task RestoreAsync(string tableName, object primaryKey, int version)
        {
            // No restore functionality in this implementation.
            return Task.CompletedTask;
        }
    }
}