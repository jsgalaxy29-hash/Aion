using System.Collections.Generic;
using System.Threading.Tasks;
using Aion.DataEngine.Entities;

namespace Aion.DataEngine.Interfaces
{
    /// <summary>
    /// Defines a contract for providers that can read from and write to
    /// external data sources.  Each implementation corresponds to a
    /// particular source type (REST API, CSV, Excel, S3, GED, etc.).  The
    /// provider receives the configuration of the source and the binding
    /// information describing how to locate the data and how to map it
    /// back into a dynamic table.
    /// </summary>
    public interface IExternalSourceProvider
    {
        /// <summary>
        /// Gets the string identifier of the source type handled by this
        /// provider (e.g. "REST_API", "CSV", "EXCEL", "S3", "GED").  This
        /// value should match the <see cref="SSourceExterne.Type"/> stored in
        /// the metadata.
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Reads data from the external source according to the given
        /// binding.  The provider may use the context (input parameters
        /// passed by the caller) to tailor the request (for example
        /// specifying query parameters or filters).  The result is a
        /// collection of dictionaries mapping column names to values.
        /// </summary>
        /// <param name="source">The external source configuration.</param>
        /// <param name="binding">The binding describing how to extract data.</param>
        /// <param name="context">A dictionary of context values (may be null).</param>
        /// <returns>A collection of rows, each represented by a dictionary.
        /// </returns>
        Task<IEnumerable<IDictionary<string, object?>>> ReadAsync(SSourceExterne source, SSourceBinding binding, IDictionary<string, object?>? context);

        /// <summary>
        /// Writes data to the external source according to the given
        /// binding.  The provider may ignore mappings not relevant for
        /// writing.  Implementations can throw <see cref="NotSupportedException"/>
        /// if the source is read-only.
        /// </summary>
        /// <param name="source">The external source configuration.</param>
        /// <param name="binding">The binding describing how to write data.</param>
        /// <param name="rows">The collection of rows to write.</param>
        Task WriteAsync(SSourceExterne source, SSourceBinding binding, IEnumerable<IDictionary<string, object?>> rows);
    }
}