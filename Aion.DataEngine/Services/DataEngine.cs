using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aion.DataEngine.Entities;
using Aion.DataEngine.Interfaces;

namespace Aion.DataEngine.Services
{
    /// <summary>
    /// Concrete implementation of <see cref="IDataEngine"/>.  This class
    /// orchestrates interactions with the underlying provider and auxiliary
    /// services (cache, validation, historization) to deliver a high‑level API
    /// for dynamic tables.  It assumes the presence of S_TABLE and S_CHAMP
    /// catalog tables as defined in the provided SQL script.
    /// </summary>
    public class DataEngine : IDataEngine
    {
        private readonly IDataProvider _provider;
        private readonly IValidationService _validator;
        private readonly IHistorizationService _historizer;
        private readonly ICacheService _cache;

        /// <summary>
        /// Cache key for S_TABLE entries.
        /// </summary>
        private const string TablesCacheKey = "Aion.DataEngine.Tables";

        /// <summary>
        /// Cache key for S_CHAMP entries.  The table name will be appended.
        /// </summary>
        private const string ChampsCacheKeyPrefix = "Aion.DataEngine.Champs.";

        /// <summary>
        /// Initializes a new instance of the <see cref="DataEngine"/> class.
        /// </summary>
        public DataEngine(IDataProvider provider, IValidationService validator, IHistorizationService historizer, ICacheService cache)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _historizer = historizer ?? throw new ArgumentNullException(nameof(historizer));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <inheritdoc />
        public async Task SynchronizeCatalogAsync()
        {
            // Retrieve all user tables from the database excluding system tables and the catalog itself.
            const string tablesQuery = @"
                SELECT name
                FROM sys.tables
                WHERE type = 'U' AND name NOT LIKE 'S_%'";
            var tables = await _provider.ExecuteQueryAsync(tablesQuery).ConfigureAwait(false);

            foreach (DataRow row in tables.Rows)
            {
                var tableName = row["name"].ToString()!;

                // Insert table metadata into S_TABLE if it does not already exist.
                // Use an upsert pattern: first check for existence.
                const string checkTableExistsSql = "SELECT Id FROM S_TABLE WHERE LIBELLE = @name";
                var existing = await _provider.ExecuteQueryAsync(checkTableExistsSql, new Dictionary<string, object?> { ["@name"] = tableName }).ConfigureAwait(false);
                int tableId;
                if (existing.Rows.Count == 0)
                {
                    const string insertTableSql = @"
                        INSERT INTO S_TABLE (
                            LIBELLE, DESCRIPTION, PARENT, PARENT_LIAISON, REFERENTIEL_LIBELLE, TYPE,
                            DOC, ACTIF, DELETED, IS_HISTORISE,
                            DT_CREATION, DT_MODIFICATION, DT_SUPPRESSION, USR_CREATION_ID, USR_MODIFICATION_ID)
                        VALUES (
                            @libelle, @description, NULL, NULL, NULL, 'F',
                            0, 1, 0, 1,
                            GETDATE(), GETDATE(), GETDATE(), 1, 1
                        );
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    var param = new Dictionary<string, object?> { ["@libelle"] = tableName, ["@description"] = (object?)null };
                    var dt = await _provider.ExecuteQueryAsync(insertTableSql, param).ConfigureAwait(false);
                    tableId = Convert.ToInt32(dt.Rows[0][0]);
                }
                else
                {
                    tableId = Convert.ToInt32(existing.Rows[0][0]);
                }

                // Synchronize columns for this table.
                const string columnsQuery = @"
                    SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = @tableName";
                var columns = await _provider.ExecuteQueryAsync(columnsQuery, new Dictionary<string, object?> { ["@tableName"] = tableName }).ConfigureAwait(false);

                foreach (DataRow colRow in columns.Rows)
                {
                    var colName = colRow["COLUMN_NAME"].ToString()!;
                    var dataType = colRow["DATA_TYPE"].ToString()!;
                    var maxLengthObj = colRow["CHARACTER_MAXIMUM_LENGTH"];
                    var maxLength = maxLengthObj != DBNull.Value ? Convert.ToInt32(maxLengthObj) : 0;
                    var isNullable = string.Equals(colRow["IS_NULLABLE"].ToString(), "YES", StringComparison.OrdinalIgnoreCase);

                    const string checkColumnSql = "SELECT Id FROM S_CHAMP WHERE TABLE_ID = @tableId AND LIBELLE = @colName";
                    var exists = await _provider.ExecuteQueryAsync(checkColumnSql, new Dictionary<string, object?> {
                        ["@tableId"] = tableId,
                        ["@colName"] = colName
                    }).ConfigureAwait(false);
                    if (exists.Rows.Count == 0)
                    {
                        // Insert column metadata.  We assume no primary key or unique constraints for discovered columns.
                        const string insertColSql = @"
                            INSERT INTO S_CHAMP (
                                TABLE_ID, LIBELLE, ALIAS, DATA_TYPE,
                                IS_CLE_PRIMAIRE, IS_UNIQUE, TAILLE, REFERENTIEL,
                                REFERENTIEL_WHERE_CLAUSE, DEFAUT, IS_NULLEABLE,
                                MIN, MAX, REGEX, IS_VISIBLE, ORDRE,
                                FORMAT, MASQUE, IS_LINK_TO_BDD, IS_SEARCH,
                                SEARCH_OPERATOR, SEARCH_DEFAUT_VALUE,
                                COORDONNEE_X, COORDONNEE_Y, COORDONNEE_LABEL_X, COORDONNEE_LABEL_Y, COMMENTAIRE,
                                DOC, ACTIF, DELETED, IS_HISTORISE,
                                SCRIPT_VALIDATION, YAML_VALIDATION,
                                DT_CREATION, DT_MODIFICATION, DT_SUPPRESSION, USR_CREATION_ID, USR_MODIFICATION_ID)
                            VALUES (
                                @tableId, @libelle, @alias, @dataType,
                                0, 0, @taille, NULL,
                                NULL, NULL, @isNullable,
                                NULL, NULL, NULL, 1, NULL,
                                NULL, NULL, 1, 0,
                                NULL, NULL,
                                0, 0, 0, 0, NULL,
                                0, 1, 0, 1,
                                NULL, NULL,
                                GETDATE(), GETDATE(), GETDATE(), 1, 1
                            );
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";
                        var colParams = new Dictionary<string, object?>
                        {
                            ["@tableId"] = tableId,
                            ["@libelle"] = colName,
                            ["@alias"] = (object?)null,
                            ["@dataType"] = dataType,
                            ["@taille"] = maxLength,
                            ["@isNullable"] = isNullable ? 1 : 0
                        };
                        await _provider.ExecuteQueryAsync(insertColSql, colParams).ConfigureAwait(false);
                    }
                }
            }

            // Invalidate cached metadata.
            await _cache.SetAsync(TablesCacheKey, (IList<STable>?)null!, TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task CreatePhysicalTableAsync(STable table, IEnumerable<SChamp> fields)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            // Build CREATE TABLE SQL dynamically.
            var builder = new StringBuilder();
            builder.Append($"CREATE TABLE [{table.Libelle}] (\n");
            var pkColumns = new List<string>();
            var fieldList = fields.ToList();
            for (int i = 0; i < fieldList.Count; i++)
            {
                var f = fieldList[i];
                builder.Append($"    [{f.Libelle}] {BuildSqlDataType(f)} {BuildNullability(f)}");
                if (!string.IsNullOrEmpty(f.Defaut))
                {
                    builder.Append($" DEFAULT {FormatDefaultValue(f)}");
                }
                if (f.IsUnique)
                {
                    builder.Append(" UNIQUE");
                }
                if (i < fieldList.Count - 1 || fieldList.Any(ff => ff.IsClePrimaire))
                {
                    builder.AppendLine(",");
                }
                else
                {
                    builder.AppendLine();
                }
                if (f.IsClePrimaire)
                {
                    pkColumns.Add(f.Libelle);
                }
            }
            if (pkColumns.Count > 0)
            {
                builder.AppendLine($"    CONSTRAINT PK_{table.Libelle} PRIMARY KEY ({string.Join(", ", pkColumns.Select(c => $"[{c}]"))})");
            }
            builder.AppendLine(")");

            var createSql = builder.ToString();
            await _provider.ExecuteNonQueryAsync(createSql).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<DataTable> GetAllAsync(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            var sql = $"SELECT * FROM [{tableName}]";
            return await _provider.ExecuteQueryAsync(sql).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IDictionary<string, object?>> GetByIdAsync(string tableName, string primaryKeyName, object id)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (string.IsNullOrWhiteSpace(primaryKeyName)) throw new ArgumentNullException(nameof(primaryKeyName));
            var sql = $"SELECT * FROM [{tableName}] WHERE [{primaryKeyName}] = @id";
            var result = await _provider.ExecuteQueryAsync(sql, new Dictionary<string, object?> { ["@id"] = id }).ConfigureAwait(false);
            if (result.Rows.Count == 0) return new Dictionary<string, object?>();
            return result.Rows[0].Table.Columns.Cast<DataColumn>().ToDictionary(col => col.ColumnName, col => result.Rows[0][col]);
        }

        /// <inheritdoc />
        public async Task<int> InsertAsync(string tableName, IDictionary<string, object?> values)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (values == null || values.Count == 0) throw new ArgumentException("Values cannot be null or empty", nameof(values));
            var columns = values.Keys.ToArray();
            var parameters = columns.Select(c => "@" + c).ToArray();
            var sql = $"INSERT INTO [{tableName}] ({string.Join(", ", columns.Select(c => $"[{c}]"))}) VALUES ({string.Join(", ", parameters)})";
            var paramDict = values.ToDictionary(kvp => "@" + kvp.Key, kvp => kvp.Value);
            var affected = await _provider.ExecuteNonQueryAsync(sql, paramDict).ConfigureAwait(false);
            // Record history only if enabled on the table.  Since the record
            // is new we don't know its identity value; pass primary key as 0.
            var tableMeta = await GetTableMetadataAsync(tableName).ConfigureAwait(false);
            if (tableMeta?.IsHistorise == true)
            {
                var fieldsMeta = await GetFieldsMetadataAsync(tableMeta.Id).ConfigureAwait(false);
                var newValues = new Dictionary<string, object?>();
                foreach (var field in fieldsMeta)
                {
                    if (!field.IsHistorise) continue;
                    if (values.TryGetValue(field.Libelle, out var val))
                    {
                        newValues[field.Libelle] = val;
                    }
                }
                await _historizer.SaveHistoryAsync(tableName, "INSERT", 0, newValues, null).ConfigureAwait(false);
            }
            return affected;
        }

        /// <inheritdoc />
        public async Task<int> UpdateAsync(string tableName, string primaryKeyName, object id, IDictionary<string, object?> values)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (string.IsNullOrWhiteSpace(primaryKeyName)) throw new ArgumentNullException(nameof(primaryKeyName));
            if (values == null || values.Count == 0) throw new ArgumentException("Values cannot be null or empty", nameof(values));
            var setClauses = values.Keys.Select(k => $"[{k}] = @{k}").ToArray();
            var sql = $"UPDATE [{tableName}] SET {string.Join(", ", setClauses)} WHERE [{primaryKeyName}] = @id";
            var paramDict = values.ToDictionary(kvp => "@" + kvp.Key, kvp => kvp.Value);
            paramDict.Add("@id", id);
            // Determine if historisation is enabled and fetch existing row before update if needed
            var tableMeta = await GetTableMetadataAsync(tableName).ConfigureAwait(false);
            IDictionary<string, object?>? existingRow = null;
            IList<SChamp>? fieldsMetaForHistory = null;
            if (tableMeta?.IsHistorise == true)
            {
                fieldsMetaForHistory = await GetFieldsMetadataAsync(tableMeta.Id).ConfigureAwait(false);
                existingRow = await GetByIdAsync(tableName, primaryKeyName, id).ConfigureAwait(false);
            }
            // Execute the update
            var affected = await _provider.ExecuteNonQueryAsync(sql, paramDict).ConfigureAwait(false);
            // Record history only if enabled on the table
            if (tableMeta?.IsHistorise == true && fieldsMetaForHistory != null && existingRow != null)
            {
                var newValues = new Dictionary<string, object?>();
                var oldValues = new Dictionary<string, object?>();
                foreach (var field in fieldsMetaForHistory)
                {
                    if (!field.IsHistorise) continue;
                    if (values.TryGetValue(field.Libelle, out var newVal))
                    {
                        newValues[field.Libelle] = newVal;
                        if (existingRow.TryGetValue(field.Libelle, out var oldVal))
                        {
                            oldValues[field.Libelle] = oldVal;
                        }
                    }
                }
                await _historizer.SaveHistoryAsync(tableName, "UPDATE", id, newValues, oldValues).ConfigureAwait(false);
            }
            return affected;
        }

        /// <inheritdoc />
        public async Task<int> DeleteAsync(string tableName, string primaryKeyName, object id)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (string.IsNullOrWhiteSpace(primaryKeyName)) throw new ArgumentNullException(nameof(primaryKeyName));
            var sql = $"DELETE FROM [{tableName}] WHERE [{primaryKeyName}] = @id";
            // Determine if historisation is enabled
            var tableMeta = await GetTableMetadataAsync(tableName).ConfigureAwait(false);
            IDictionary<string, object?>? existingRow = null;
            IList<SChamp>? fieldsMetaForHistory = null;
            if (tableMeta?.IsHistorise == true)
            {
                fieldsMetaForHistory = await GetFieldsMetadataAsync(tableMeta.Id).ConfigureAwait(false);
                // Fetch existing values before deletion only if history is enabled
                existingRow = await GetByIdAsync(tableName, primaryKeyName, id).ConfigureAwait(false);
            }
            // Perform the deletion
            var affected = await _provider.ExecuteNonQueryAsync(sql, new Dictionary<string, object?> { ["@id"] = id }).ConfigureAwait(false);
            // Record history if enabled
            if (tableMeta?.IsHistorise == true && fieldsMetaForHistory != null && existingRow != null)
            {
                var oldValues = new Dictionary<string, object?>();
                foreach (var field in fieldsMetaForHistory)
                {
                    if (!field.IsHistorise) continue;
                    if (existingRow.TryGetValue(field.Libelle, out var oldVal))
                    {
                        oldValues[field.Libelle] = oldVal;
                    }
                }
                await _historizer.SaveHistoryAsync(tableName, "DELETE", id, new Dictionary<string, object?>(), oldValues).ConfigureAwait(false);
            }
            return affected;
        }

        #region Helpers

        /// <summary>
        /// Builds the SQL data type clause for a column.
        /// Handles length for character types and leaves other types unchanged.
        /// </summary>
        private static string BuildSqlDataType(SChamp field)
        {
            var type = field.DataType.Trim().ToUpperInvariant();
            // If the type has parentheses already (e.g. DECIMAL(10,2)) we respect it.
            if (type.Contains("(")) return type;
            if ((type.Contains("CHAR") || type.Contains("VARCHAR")) && field.Taille > 0)
            {
                return $"{type}({field.Taille})";
            }
            return type;
        }

        /// <summary>
        /// Returns the NULL/NOT NULL clause.
        /// </summary>
        private static string BuildNullability(SChamp field) => field.IsNulleable ? "NULL" : "NOT NULL";

        /// <summary>
        /// Formats a default value for inclusion in an SQL statement.
        /// Detects string values and wraps them in quotes.
        /// </summary>
        private static string FormatDefaultValue(SChamp field)
        {
            if (string.IsNullOrWhiteSpace(field.Defaut)) return string.Empty;
            // Try to detect if the default is numeric or date; otherwise quote as string.
            var def = field.Defaut!.Trim();
            if (decimal.TryParse(def, out _) || DateTime.TryParse(def, out _))
            {
                return def;
            }
            // Wrap string with single quotes and escape single quotes within
            return $"'" + def.Replace("'", "''") + "'";
        }

        /// <summary>
        /// Retrieves metadata for a table (S_TABLE) by its physical name.  Uses
        /// an in‑memory cache to avoid repeated lookups.
        /// </summary>
        /// <param name="tableName">Physical table name (LIBELLE).</param>
        private async Task<STable?> GetTableMetadataAsync(string tableName)
        {
            var cacheKey = $"{TablesCacheKey}:{tableName}";
            var cached = await _cache.GetAsync<STable>(cacheKey).ConfigureAwait(false);
            if (cached != null)
            {
                return cached;
            }
            // Query S_TABLE to obtain the table definition
            const string sql = "SELECT Id, LIBELLE, DESCRIPTION, PARENT, PARENT_LIAISON, REFERENTIEL_LIBELLE, TYPE, DOC, ACTIF, DELETED, IS_HISTORISE FROM S_TABLE WHERE LIBELLE = @name";
            var dt = await _provider.ExecuteQueryAsync(sql, new Dictionary<string, object?> { ["@name"] = tableName }).ConfigureAwait(false);
            if (dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            var table = new STable
            {
                Id = Convert.ToInt32(row["Id"]),
                Libelle = row["LIBELLE"].ToString()!,
                Description = row["DESCRIPTION"] as string,
                Parent = row["PARENT"] as string,
                ParentLiaison = row["PARENT_LIAISON"] as string,
                ReferentielLibelle = row["REFERENTIEL_LIBELLE"] as string,
                Type = row["TYPE"] as string,
                Doc = Convert.ToBoolean(row["DOC"]),
                Actif = Convert.ToBoolean(row["ACTIF"]),
                Deleted = Convert.ToBoolean(row["DELETED"]),
                IsHistorise = row.Table.Columns.Contains("IS_HISTORISE") && Convert.ToBoolean(row["IS_HISTORISE"])
            };
            await _cache.SetAsync(cacheKey, table, TimeSpan.FromMinutes(5)).ConfigureAwait(false);
            return table;
        }

        /// <summary>
        /// Retrieves metadata for all fields (S_CHAMP) belonging to a table.
        /// Uses an in‑memory cache to avoid repeated lookups.
        /// </summary>
        /// <param name="tableId">Identifier of the table (S_TABLE.Id).</param>
        private async Task<IList<SChamp>> GetFieldsMetadataAsync(int tableId)
        {
            var cacheKey = ChampsCacheKeyPrefix + tableId;
            var cached = await _cache.GetAsync<IList<SChamp>>(cacheKey).ConfigureAwait(false);
            if (cached != null)
            {
                return cached;
            }
            const string sql = @"
                SELECT Id, TABLE_ID, LIBELLE, ALIAS, DATA_TYPE, IS_CLE_PRIMAIRE, IS_UNIQUE, TAILLE,
                       REFERENTIEL, REFERENTIEL_WHERE_CLAUSE, DEFAUT, IS_NULLEABLE,
                       MIN, MAX, REGEX, IS_VISIBLE, ORDRE, FORMAT, MASQUE,
                       IS_LINK_TO_BDD, IS_SEARCH, SEARCH_OPERATOR, SEARCH_DEFAUT_VALUE,
                       COORDONNEE_X, COORDONNEE_Y, COORDONNEE_LABEL_X, COORDONNEE_LABEL_Y, COMMENTAIRE,
                       DOC, ACTIF, DELETED, IS_HISTORISE,
                       SCRIPT_VALIDATION, YAML_VALIDATION
                FROM S_CHAMP
                WHERE TABLE_ID = @tableId";
            var dt = await _provider.ExecuteQueryAsync(sql, new Dictionary<string, object?> { ["@tableId"] = tableId }).ConfigureAwait(false);
            var champs = new List<SChamp>(dt.Rows.Count);
            foreach (DataRow row in dt.Rows)
            {
                champs.Add(new SChamp
                {
                    Id = Convert.ToInt32(row["Id"]),
                    TableId = Convert.ToInt32(row["TABLE_ID"]),
                    Libelle = row["LIBELLE"].ToString()!,
                    Alias = row["ALIAS"] as string,
                    DataType = row["DATA_TYPE"].ToString()!,
                    IsClePrimaire = Convert.ToBoolean(row["IS_CLE_PRIMAIRE"]),
                    IsUnique = Convert.ToBoolean(row["IS_UNIQUE"]),
                    Taille = Convert.ToInt32(row["TAILLE"]),
                    Referentiel = row["REFERENTIEL"] as string,
                    ReferentielWhereClause = row["REFERENTIEL_WHERE_CLAUSE"] as string,
                    Defaut = row["DEFAUT"] as string,
                    IsNulleable = Convert.ToBoolean(row["IS_NULLEABLE"]),
                    Min = row["MIN"] as string,
                    Max = row["MAX"] as string,
                    Regex = row["REGEX"] as string,
                    IsVisible = Convert.ToBoolean(row["IS_VISIBLE"]),
                    Ordre = row["ORDRE"] != DBNull.Value ? (int?)Convert.ToInt32(row["ORDRE"]) : null,
                    Format = row["FORMAT"] as string,
                    Masque = row["MASQUE"] as string,
                    IsLinkToBdd = Convert.ToBoolean(row["IS_LINK_TO_BDD"]),
                    IsSearch = Convert.ToBoolean(row["IS_SEARCH"]),
                    SearchOperator = row["SEARCH_OPERATOR"] as string,
                    SearchDefautValue = row["SEARCH_DEFAUT_VALUE"] as string,
                    CoordonneeX = Convert.ToInt32(row["COORDONNEE_X"]),
                    CoordonneeY = Convert.ToInt32(row["COORDONNEE_Y"]),
                    CoordonneeLabelX = Convert.ToInt32(row["COORDONNEE_LABEL_X"]),
                    CoordonneeLabelY = Convert.ToInt32(row["COORDONNEE_LABEL_Y"]),
                    Commentaire = row["COMMENTAIRE"] as string,
                    Doc = Convert.ToBoolean(row["DOC"]),
                    Actif = Convert.ToBoolean(row["ACTIF"]),
                    Deleted = Convert.ToBoolean(row["DELETED"]),
                    IsHistorise = row.Table.Columns.Contains("IS_HISTORISE") && Convert.ToBoolean(row["IS_HISTORISE"]),
                    ValidationScript = row.Table.Columns.Contains("SCRIPT_VALIDATION") ? row["SCRIPT_VALIDATION"] as string : null,
                    ValidationYaml = row.Table.Columns.Contains("YAML_VALIDATION") ? row["YAML_VALIDATION"] as string : null
                });
            }
            await _cache.SetAsync(cacheKey, champs, TimeSpan.FromMinutes(5)).ConfigureAwait(false);
            return champs;
        }
        #endregion
    }
}