using System.Data;
using System.Threading.Tasks;

namespace Aion.DataEngine.Interfaces
{
    /// <summary>
    /// Provides a simple abstraction over the underlying database technology.  
    /// Each provider handles its own connections, parameter syntax and command execution.
    /// </summary>
    public interface IDataProvider
    {
        /// <summary>
        /// Executes a non‑query command (e.g. CREATE TABLE, INSERT, UPDATE, DELETE).
        /// </summary>
        /// <param name="commandText">The SQL command to execute.</param>
        /// <param name="parameters">Optional named parameters.</param>
        /// <returns>The number of affected rows.</returns>
        Task<int> ExecuteNonQueryAsync(string commandText, IDictionary<string, object?>? parameters = null);

        /// <summary>
        /// Executes a query and returns the results as a DataTable.
        /// </summary>
        /// <param name="commandText">The SQL query to execute.</param>
        /// <param name="parameters">Optional named parameters.</param>
        /// <returns>A DataTable containing the result set.</returns>
        Task<DataTable> ExecuteQueryAsync(string commandText, IDictionary<string, object?>? parameters = null);

        /// <summary>
        /// Begins a new database transaction.
        /// </summary>
        /// <returns>An object implementing <see cref="IDbTransaction"/>.</returns>
        Task<IDbTransaction> BeginTransactionAsync();

        /// <summary>
        /// Gets the underlying connection string (useful for EF Core options).
        /// </summary>
        string ConnectionString { get; }
    }
}