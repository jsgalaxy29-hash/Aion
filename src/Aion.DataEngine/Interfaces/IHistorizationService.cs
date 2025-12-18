using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aion.DataEngine.Interfaces
{
    using System;
    using Aion.DataEngine.Entities;
    /// <summary>
    /// Provides a hook for storing audit/history records when data is mutated.
    /// Implementations of this service should write entries into a history table
    /// such as S_HISTORIQUE.
    /// </summary>
    public interface IHistorizationService
    {
        /// <summary>
        /// Persists history records representing a mutation on a table.  The
        /// service should record the old and new values for each field as
        /// appropriate.  For inserts, <paramref name="oldValues"/> is null.
        /// For deletions, <paramref name="newValues"/> may be empty.
        /// </summary>
        /// <param name="tableName">The physical table name being modified.</param>
        /// <param name="operation">The operation (INSERT, UPDATE, DELETE).</param>
        /// <param name="primaryKey">The primary key value affected.</param>
        /// <param name="newValues">The record values after the operation (for insert/update).  May be empty for delete.</param>
        /// <param name="oldValues">The record values before the operation (for update/delete).  Null for inserts.</param>
        Task SaveHistoryAsync(string tableName, string operation, object primaryKey, IDictionary<string, object?> newValues, IDictionary<string, object?>? oldValues = null);

        /// <summary>
        /// Retrieves history entries for a given record.  Implementations
        /// should return entries ordered by version ascending.
        /// </summary>
        /// <param name="tableName">The physical table name.</param>
        /// <param name="primaryKey">The primary key of the record.</param>
        /// <returns>A list of history entries.</returns>
        Task<IList<SHistorique>> GetHistoryAsync(string tableName, object primaryKey);

        /// <summary>
        /// Restores a record to the specified version.  Implementations
        /// should update the underlying table with the values from the
        /// specified version and record a new history entry reflecting the
        /// restoration.
        /// </summary>
        /// <param name="tableName">The physical table name.</param>
        /// <param name="primaryKey">The primary key of the record.</param>
        /// <param name="version">The version number to restore.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RestoreAsync(string tableName, object primaryKey, int version);
    }
}