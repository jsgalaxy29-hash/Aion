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
    /// Concrete implementation of <see cref="IHistorizationService"/> that
    /// records detailed history for each field modification.  Entries are
    /// written into the table S_HISTORIQUE.  This service can also
    /// retrieve the history for a given record.  Restore functionality is
    /// left unimplemented as it requires careful handling of business
    /// logic and transactions.
    /// </summary>
    public class AdvancedHistorizationService : IHistorizationService
    {
        private readonly IDataProvider _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdvancedHistorizationService"/>.
        /// </summary>
        /// <param name="provider">The data provider used to execute SQL statements.</param>
        public AdvancedHistorizationService(IDataProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <inheritdoc />
        public async Task SaveHistoryAsync(string tableName, string operation, object primaryKey, IDictionary<string, object?> newValues, IDictionary<string, object?>? oldValues = null)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (primaryKey == null) throw new ArgumentNullException(nameof(primaryKey));
            if (newValues == null) throw new ArgumentNullException(nameof(newValues));

            var recordId = primaryKey.ToString() ?? string.Empty;
            // Determine current maximum version for this record.
            const string verSql = "SELECT ISNULL(MAX([VERSION]),0) AS MaxVer FROM S_HISTORIQUE WHERE TABLE_NAME = @table AND RECORD_ID = @id";
            var verParams = new Dictionary<string, object?> { ["@table"] = tableName, ["@id"] = recordId };
            int currentVer = 0;
            var verDt = await _provider.ExecuteQueryAsync(verSql, verParams).ConfigureAwait(false);
            if (verDt.Rows.Count > 0)
            {
                currentVer = Convert.ToInt32(verDt.Rows[0]["MaxVer"]);
            }
            int nextVer = currentVer + 1;

            // If operation is DELETE, newValues may be empty and oldValues contains previous values.
            if (string.Equals(operation, "DELETE", StringComparison.OrdinalIgnoreCase))
            {
                if (oldValues != null)
                {
                    foreach (var kvp in oldValues)
                    {
                        var fieldName = kvp.Key;
                        var oldValStr = kvp.Value?.ToString();
                        const string insSqlDel = "INSERT INTO S_HISTORIQUE (TABLE_NAME, RECORD_ID, FIELD_NAME, OLD_VALUE, NEW_VALUE, OPERATION, VERSION, DT_MODIFICATION, USR_ID) VALUES (@table, @id, @field, @oldValue, NULL, @op, @version, GETDATE(), @usr)";
                        var insParams = new Dictionary<string, object?>
                        {
                            ["@table"] = tableName,
                            ["@id"] = recordId,
                            ["@field"] = fieldName,
                            ["@oldValue"] = oldValStr,
                            ["@op"] = operation,
                            ["@version"] = nextVer,
                            ["@usr"] = 0
                        };
                        await _provider.ExecuteNonQueryAsync(insSqlDel, insParams).ConfigureAwait(false);
                    }
                }
                return;
            }

            // For INSERT and UPDATE operations, record changes per field.  If
            // oldValues is provided, compare old and new; otherwise treat all
            // fields as new (e.g. INSERT).
            foreach (var kvp in newValues)
            {
                var fieldName = kvp.Key;
                var newValStr = kvp.Value?.ToString();
                string? oldValStr = null;
                if (oldValues != null && oldValues.TryGetValue(fieldName, out var oldVal))
                {
                    oldValStr = oldVal?.ToString();
                }
                // Skip unchanged values during update.
                if (oldValStr == newValStr) continue;
                const string insSql = "INSERT INTO S_HISTORIQUE (TABLE_NAME, RECORD_ID, FIELD_NAME, OLD_VALUE, NEW_VALUE, OPERATION, VERSION, DT_MODIFICATION, USR_ID) VALUES (@table, @id, @field, @oldValue, @newValue, @op, @version, GETDATE(), @usr)";
                var insParams = new Dictionary<string, object?>
                {
                    ["@table"] = tableName,
                    ["@id"] = recordId,
                    ["@field"] = fieldName,
                    ["@oldValue"] = oldValStr,
                    ["@newValue"] = newValStr,
                    ["@op"] = operation,
                    ["@version"] = nextVer,
                    ["@usr"] = 0
                };
                await _provider.ExecuteNonQueryAsync(insSql, insParams).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<IList<SHistorique>> GetHistoryAsync(string tableName, object primaryKey)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (primaryKey == null) throw new ArgumentNullException(nameof(primaryKey));
            var recordId = primaryKey.ToString() ?? string.Empty;
            const string sql = "SELECT ID, TABLE_NAME, RECORD_ID, FIELD_NAME, OLD_VALUE, NEW_VALUE, OPERATION, VERSION, DT_MODIFICATION, USR_ID FROM S_HISTORIQUE WHERE TABLE_NAME = @table AND RECORD_ID = @id ORDER BY VERSION ASC, ID ASC";
            var dt = await _provider.ExecuteQueryAsync(sql, new Dictionary<string, object?> { ["@table"] = tableName, ["@id"] = recordId }).ConfigureAwait(false);
            var list = new List<SHistorique>(dt.Rows.Count);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new SHistorique
                {
                    Id = Convert.ToInt32(row["ID"]),
                    TableName = row["TABLE_NAME"].ToString() ?? string.Empty,
                    RecordId = row["RECORD_ID"].ToString() ?? string.Empty,
                    FieldName = row["FIELD_NAME"] as string,
                    OldValue = row["OLD_VALUE"] as string,
                    NewValue = row["NEW_VALUE"] as string,
                    Operation = row["OPERATION"].ToString() ?? string.Empty,
                    Version = Convert.ToInt32(row["VERSION"]),
                    DtModification = Convert.ToDateTime(row["DT_MODIFICATION"]),
                    UserId = Convert.ToInt32(row["USR_ID"])
                });
            }
            return list;
        }

        /// <inheritdoc />
        public Task RestoreAsync(string tableName, object primaryKey, int version)
        {
            // Implementing a generic restore mechanism requires knowledge of the
            // table's primary key and business constraints.  For now this
            // operation is not supported.
            throw new NotImplementedException("RestoreAsync is not implemented in this version.");
        }
    }
}