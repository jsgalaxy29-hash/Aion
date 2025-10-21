using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Aion.DataEngine.Entities;
using Aion.DataEngine.Interfaces;

namespace Aion.DataEngine.Services
{
    /// <summary>
    /// Provides high‑level access to external sources based on metadata stored
    /// in the tables S_SOURCE_EXTERNE and S_SOURCE_BINDING.  This service
    /// looks up the source and binding definitions, resolves the
    /// appropriate provider, and delegates the actual data transfer to the
    /// provider.  It abstracts away the details of reading from different
    /// types of sources.
    /// </summary>
    public class ExternalSourceService
    {
        private readonly IDataProvider _dataProvider;
        private readonly IDictionary<string, IExternalSourceProvider> _providers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalSourceService"/>.
        /// </summary>
        /// <param name="dataProvider">The provider used to query the metadata tables.</param>
        /// <param name="providers">The collection of external source providers.  Each
        /// provider must have a unique <see cref="IExternalSourceProvider.Type"/> value.</param>
        public ExternalSourceService(IDataProvider dataProvider, IEnumerable<IExternalSourceProvider> providers)
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
            if (providers == null) throw new ArgumentNullException(nameof(providers));
            _providers = providers.ToDictionary(p => p.Type, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Reads data from the external source bound to the specified table and
        /// optional column.  If no binding exists, an empty collection is returned.
        /// </summary>
        /// <param name="tableId">Identifier of the logical table (S_TABLE.Id).</param>
        /// <param name="champId">Optional identifier of the logical column (S_CHAMP.Id).</param>
        /// <param name="context">Optional context parameters passed to the provider.</param>
        /// <returns>A collection of rows returned by the provider.  Each row
        /// is represented as a dictionary mapping column names to values.</returns>
        public async Task<IEnumerable<IDictionary<string, object?>>> ReadAsync(int tableId, int? champId, IDictionary<string, object?>? context = null)
        {
            // Retrieve binding for this table/column.
            var binding = await GetBindingAsync(tableId, champId).ConfigureAwait(false);
            if (binding == null)
            {
                return Enumerable.Empty<IDictionary<string, object?>>();
            }
            // Retrieve the source configuration.
            var source = await GetSourceByIdAsync(binding.SourceId).ConfigureAwait(false);
            if (source == null || !_providers.TryGetValue(source.Type, out var provider))
            {
                return Enumerable.Empty<IDictionary<string, object?>>();
            }
            // Delegate to provider.
            return await provider.ReadAsync(source, binding, context).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes data to the external source bound to the specified table and
        /// optional column.  If no binding exists or the provider does not
        /// support writing, the call may silently do nothing or throw a
        /// <see cref="NotSupportedException"/> depending on the provider implementation.
        /// </summary>
        /// <param name="tableId">Identifier of the logical table.</param>
        /// <param name="champId">Optional identifier of the logical column.</param>
        /// <param name="rows">The rows to write.</param>
        public async Task WriteAsync(int tableId, int? champId, IEnumerable<IDictionary<string, object?>> rows)
        {
            var binding = await GetBindingAsync(tableId, champId).ConfigureAwait(false);
            if (binding == null) return;
            var source = await GetSourceByIdAsync(binding.SourceId).ConfigureAwait(false);
            if (source == null || !_providers.TryGetValue(source.Type, out var provider)) return;
            await provider.WriteAsync(source, binding, rows).ConfigureAwait(false);
        }

        /// <summary>
        /// Queries the S_SOURCE_BINDING table for a binding matching the given
        /// table and column identifiers.  The query first attempts to find
        /// a binding for the specific column; if none is found, it falls
        /// back to a binding at the table level (where CHAMP_ID is NULL).
        /// </summary>
        private async Task<SSourceBinding?> GetBindingAsync(int tableId, int? champId)
        {
            var sql = @"SELECT TOP 1 ID, SOURCE_ID, TABLE_ID, CHAMP_ID, EXTRACTION_PATH, MODE, MAPPING_JSON
                         FROM S_SOURCE_BINDING
                         WHERE TABLE_ID = @tableId AND ((@champId IS NULL AND CHAMP_ID IS NULL) OR CHAMP_ID = @champId)";
            var dt = await _dataProvider.ExecuteQueryAsync(sql, new Dictionary<string, object?>
            {
                ["@tableId"] = tableId,
                ["@champId"] = champId
            }).ConfigureAwait(false);
            if (dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new SSourceBinding
            {
                Id = Convert.ToInt32(row["ID"]),
                SourceId = Convert.ToInt32(row["SOURCE_ID"]),
                TableId = Convert.ToInt32(row["TABLE_ID"]),
                ChampId = row["CHAMP_ID"] != DBNull.Value ? Convert.ToInt32(row["CHAMP_ID"]) : (int?)null,
                ExtractionPath = row["EXTRACTION_PATH"] as string,
                Mode = row["MODE"].ToString() ?? string.Empty,
                MappingJson = row["MAPPING_JSON"] as string
            };
        }

        /// <summary>
        /// Queries the S_SOURCE_EXTERNE table for the source configuration with the
        /// given identifier.
        /// </summary>
        private async Task<SSourceExterne?> GetSourceByIdAsync(int sourceId)
        {
            var sql = @"SELECT ID, NOM, TYPE, URL, AUTH_TYPE, AUTH_PARAMS, PARAMS, ACTIF, DELETED, DT_CREATION, DT_MODIFICATION
                         FROM S_SOURCE_EXTERNE
                         WHERE ID = @id AND DELETED = 0 AND ACTIF = 1";
            var dt = await _dataProvider.ExecuteQueryAsync(sql, new Dictionary<string, object?> { ["@id"] = sourceId }).ConfigureAwait(false);
            if (dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new SSourceExterne
            {
                Id = Convert.ToInt32(row["ID"]),
                Nom = row["NOM"].ToString() ?? string.Empty,
                Type = row["TYPE"].ToString() ?? string.Empty,
                Url = row["URL"] as string,
                AuthType = row["AUTH_TYPE"] as string,
                AuthParams = row["AUTH_PARAMS"] as string,
                Params = row["PARAMS"] as string,
                Actif = Convert.ToBoolean(row["ACTIF"]),
                Deleted = Convert.ToBoolean(row["DELETED"]),
                DtCreation = Convert.ToDateTime(row["DT_CREATION"]),
                DtModification = Convert.ToDateTime(row["DT_MODIFICATION"])
            };
        }
    }
}