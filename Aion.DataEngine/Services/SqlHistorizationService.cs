using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aion.DataEngine.Entities;
using Aion.DataEngine.Interfaces;

namespace Aion.DataEngine.Services
{
    /// <summary>
    /// SQL Server implementation of <see cref="IHistorizationService"/>.
    /// Persists history entries into the tables S_HISTO_VERSION and
    /// S_HISTO_CHANGE.  This service computes version numbers per
    /// row and records field‑level diffs as well as optional record
    /// snapshots.  Restoration and retrieval operations are also supported
    /// but may be stubbed for future extension.
    /// </summary>
    public class SqlHistorizationService : IHistorizationService
    {
        private readonly IDataProvider _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlHistorizationService"/>.
        /// </summary>
        /// <param name="provider">The data provider used to execute SQL commands.</param>
        public SqlHistorizationService(IDataProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <inheritdoc />
        public async Task SaveHistoryAsync(string tableName, string operation, object primaryKey, IDictionary<string, object?> newValues, IDictionary<string, object?>? oldValues = null)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (string.IsNullOrWhiteSpace(operation)) throw new ArgumentNullException(nameof(operation));
            // Primary key as string for storage.  If null convert to empty string.
            var rowPk = primaryKey?.ToString() ?? string.Empty;
            // Determine next version number for this row.
            const string versionQuery = "SELECT ISNULL(MAX(VERSION_NUM), 0) AS MaxVer FROM S_HISTO_VERSION WHERE TABLE_NAME = @tableName AND ROW_PK = @rowPk";
            var versionDt = await _provider.ExecuteQueryAsync(versionQuery, new Dictionary<string, object?> {
                ["@tableName"] = tableName,
                ["@rowPk"] = rowPk
            }).ConfigureAwait(false);
            var nextVersion = 1;
            if (versionDt.Rows.Count > 0)
            {
                nextVersion = Convert.ToInt32(versionDt.Rows[0]["MaxVer"]) + 1;
            }
            // Prepare a JSON snapshot.  For INSERT and UPDATE operations we
            // capture the new values; for DELETE we capture the old values if provided.
            string? snapshotJson = null;
            if (operation.Equals("INSERT", StringComparison.OrdinalIgnoreCase) || operation.Equals("UPDATE", StringComparison.OrdinalIgnoreCase))
            {
                snapshotJson = JsonSerializer.Serialize(newValues);
            }
            else if (operation.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                snapshotJson = JsonSerializer.Serialize(oldValues ?? new Dictionary<string, object?>());
            }
            // Insert version record and obtain identity
            const string insertVersionSql = @"
                INSERT INTO S_HISTO_VERSION (TABLE_NAME, ROW_PK, VERSION_NUM, OPERATION, USER_ID, DT_VERSION, SNAPSHOT_JSON, COMMENTAIRE)
                VALUES (@tableName, @rowPk, @versionNum, @operation, NULL, GETDATE(), @snapshotJson, NULL);
                SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewId;";
            var insertParams = new Dictionary<string, object?>
            {
                ["@tableName"] = tableName,
                ["@rowPk"] = rowPk,
                ["@versionNum"] = nextVersion,
                ["@operation"] = operation,
                ["@snapshotJson"] = snapshotJson
            };
            var versionIdDt = await _provider.ExecuteQueryAsync(insertVersionSql, insertParams).ConfigureAwait(false);
            var versionId = Convert.ToInt32(versionIdDt.Rows[0][0]);
            // Determine changes.  For INSERT operations, old values are null; for DELETE, new values are empty.
            var changes = new List<(string Field, object? OldVal, object? NewVal)>();
            // New values may be empty for delete.  For insert and update, newValues holds changed fields.
            if (newValues != null)
            {
                foreach (var kvp in newValues)
                {
                    object? oldVal = null;
                    if (oldValues != null)
                    {
                        oldValues.TryGetValue(kvp.Key, out oldVal);
                    }
                    changes.Add((kvp.Key, oldVal, kvp.Value));
                }
            }
            // For delete operations, record old values only if provided.
            if ((operation.Equals("DELETE", StringComparison.OrdinalIgnoreCase)) && (oldValues != null))
            {
                foreach (var kvp in oldValues)
                {
                    // Only record delete changes if not already added.
                    if (!newValues.ContainsKey(kvp.Key))
                    {
                        changes.Add((kvp.Key, kvp.Value, null));
                    }
                }
            }
            // Insert each change into S_HISTO_CHANGE
            foreach (var change in changes)
            {
                const string insertChangeSql = @"
                    INSERT INTO S_HISTO_CHANGE (HISTO_VERSION_ID, CHAMP, OLD_VALUE, NEW_VALUE)
                    VALUES (@versionId, @champ, @oldValue, @newValue);";
                var changeParams = new Dictionary<string, object?>
                {
                    ["@versionId"] = versionId,
                    ["@champ"] = change.Field,
                    ["@oldValue"] = change.OldVal?.ToString(),
                    ["@newValue"] = change.NewVal?.ToString()
                };
                await _provider.ExecuteNonQueryAsync(insertChangeSql, changeParams).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<IList<SHistorique>> GetHistoryAsync(string tableName, object primaryKey)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            var rowPk = primaryKey?.ToString() ?? string.Empty;
            // Query history versions and their changes
            const string historySql = @"
                SELECT v.ID, v.VERSION_NUM, v.OPERATION, v.DT_VERSION, v.SNAPSHOT_JSON,
                       c.ID AS ChangeId, c.CHAMP, c.OLD_VALUE, c.NEW_VALUE
                FROM S_HISTO_VERSION v
                LEFT JOIN S_HISTO_CHANGE c ON v.ID = c.HISTO_VERSION_ID
                WHERE v.TABLE_NAME = @tableName AND v.ROW_PK = @rowPk
                ORDER BY v.VERSION_NUM ASC, c.ID ASC";
            var dt = await _provider.ExecuteQueryAsync(historySql, new Dictionary<string, object?> {
                ["@tableName"] = tableName,
                ["@rowPk"] = rowPk
            }).ConfigureAwait(false);
            var history = new List<SHistorique>();
            var versionDict = new Dictionary<int, SHistorique>();
            foreach (System.Data.DataRow row in dt.Rows)
            {
                var versionId = Convert.ToInt32(row["ID"]);
                if (!versionDict.TryGetValue(versionId, out var histo))
                {
                    histo = new SHistorique
                    {
                        VersionId = versionId,
                        Version = Convert.ToInt32(row["VERSION_NUM"]),
                        Operation = row["OPERATION"].ToString()!,
                        DtVersion = Convert.ToDateTime(row["DT_VERSION"]),
                        SnapshotJson = row["SNAPSHOT_JSON"] as string,
                        Changes = new List<SHistoChange>()
                    };
                    versionDict[versionId] = histo;
                    history.Add(histo);
                }
                // If a change is present, add it
                if (row["ChangeId"] != DBNull.Value)
                {
                    var change = new SHistoChange
                    {
                        Id = Convert.ToInt32(row["ChangeId"]),
                        HistoVersionId = versionId,
                        Champ = row["CHAMP"].ToString()!,
                        OldValue = row["OLD_VALUE"] as string,
                        NewValue = row["NEW_VALUE"] as string
                    };
                    histo.Changes.Add(change);
                }
            }
            return history;
        }

        /// <inheritdoc />
        public Task RestoreAsync(string tableName, object primaryKey, int version)
        {
            // For now restoration is not implemented.  This could be
            // implemented by retrieving the specified version, parsing the
            // snapshot and issuing an UPDATE via the data engine.  The
            // implementation is left to the application layer.
            throw new NotImplementedException("RestoreAsync is not yet implemented.");
        }
    }
}