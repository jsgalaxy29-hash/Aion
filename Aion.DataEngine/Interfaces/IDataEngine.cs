using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Aion.DataEngine.Entities;

namespace Aion.DataEngine.Interfaces
{
    /// <summary>
    /// Exposes high‑level operations for interacting with user‑defined tables.
    /// This abstraction hides implementation details such as raw SQL generation,
    /// caching and validation, and orchestrates transactions where required.
    /// </summary>
    public interface IDataEngine
    {
        /// <summary>
        /// Scans the underlying database for existing tables and columns and
        /// synchronises them into the catalog tables (S_TABLE, S_CHAMP).  
        /// This operation populates metadata for existing legacy tables.
        /// </summary>
        Task SynchronizeCatalogAsync();

        /// <summary>
        /// Materialises a new table in the physical database based on the
        /// provided metadata.  It is assumed that the corresponding S_TABLE and
        /// S_CHAMP records have already been created and validated.
        /// </summary>
        /// <param name="table">The table definition.</param>
        /// <param name="fields">The collection of field definitions.</param>
        Task CreatePhysicalTableAsync(STable table, IEnumerable<SField> fields);

        /// <summary>
        /// Retrieves all records from a user‑defined table.
        /// </summary>
        /// <param name="tableName">The physical table name.</param>
        /// <returns>A DataTable containing the rows.</returns>
        Task<DataTable> GetAllAsync(string tableName);

        /// <summary>
        /// Retrieves a single record by primary key.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="primaryKeyName">The name of the primary key column.</param>
        /// <param name="id">The primary key value.</param>
        /// <returns>The matching row as a dictionary.</returns>
        Task<IDictionary<string, object?>> GetByIdAsync(string tableName, string primaryKeyName, object id);

        /// <summary>
        /// Inserts a new record into a user‑defined table.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="values">A dictionary of column names to values.</param>
        Task<int> InsertAsync(string tableName, IDictionary<string, object?> values);

        /// <summary>
        /// Updates an existing record in a user‑defined table.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="primaryKeyName">The primary key column name.</param>
        /// <param name="id">The primary key value.</param>
        /// <param name="values">A dictionary of column names to updated values.</param>
        Task<int> UpdateAsync(string tableName, string primaryKeyName, object id, IDictionary<string, object?> values);

        /// <summary>
        /// Deletes a record from a user‑defined table.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="primaryKeyName">The primary key column name.</param>
        /// <param name="id">The primary key value.</param>
        Task<int> DeleteAsync(string tableName, string primaryKeyName, object id);
    }
}