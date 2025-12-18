using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Aion.DataEngine.Interfaces
{
    /// <summary>Minimal DB abstraction for the engine and services.</summary>
    public interface IDataProvider
    {
        Task<int> ExecuteNonQueryAsync(string sql, IDictionary<string, object?>? parameters = null);
        Task<object?> ExecuteScalarAsync(string sql, IDictionary<string, object?>? parameters = null);
        Task<DataTable> ExecuteQueryAsync(string sql, IDictionary<string, object?>? parameters = null);
    }
}
