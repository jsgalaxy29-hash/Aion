using Aion.DataEngine.Entities;
using Aion.DataEngine.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aion.DataEngine.Services
{
    public partial class DataEngine : IDataEngine
    {
        
        private readonly IDataProvider _db;
        private readonly IValidationService _validator;
        private readonly IHistorizationService _historizer;
        private readonly ICacheService _cache;
        private readonly IUserContext _user;
        private readonly IClock _clock;

        /// <summary>
        /// Cache key for STable entries.
        /// </summary>
        private const string TablesCacheKey = "Aion.DataEngine.Tables";

        /// <summary>
        /// Cache key for SField entries.  The table name will be appended.
        /// </summary>
        private const string ChampsCacheKeyPrefix = "Aion.DataEngine.Fields.";

        public DataEngine(IDataProvider db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db)); ;
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public DataEngine(IDataProvider db, IUserContext user, IClock clock, IValidationService validator, IHistorizationService historizer, ICacheService cache)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db)); ; 
            _user = user ?? throw new ArgumentNullException(nameof(user)); 
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _historizer = historizer ?? throw new ArgumentNullException(nameof(historizer));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Catalogs the database schema into the STable and SField metadata tables.
        /// </summary>
        /// <returns></returns>
        public async Task SynchronizeSystemCatalogAsync()
        {
            // Retrieve all user tables from the database excluding system tables and the catalog itself.
            const string tablesQuery = @"
                SELECT name
                FROM sys.tables
                WHERE type = 'U' AND name LIKE 'S%'";
            var tables = await _db.ExecuteQueryAsync(tablesQuery).ConfigureAwait(false);

            foreach (DataRow row in tables.Rows)
            {
                var tableName = row["name"].ToString()!;

                // Insert table metadata into STable if it does not already exist.
                // Use an upsert pattern: first check for existence.
                const string checkTableExistsSql = "SELECT Id FROM STable WHERE Libelle = @name";
                var existing = await _db.ExecuteQueryAsync(checkTableExistsSql, new Dictionary<string, object?> { ["@name"] = tableName }).ConfigureAwait(false);
                int tableId;
                if (existing.Rows.Count == 0)
                {
                    const string insertTableSql = @"
                        INSERT INTO STable (
                            Libelle, Description, Parent, ParentLiaison, ReferentielLibelle, Type)
                        VALUES (@libelle, @description, NULL, NULL, NULL, 'F')
                        );
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    var param = new Dictionary<string, object?> { ["@libelle"] = tableName, ["@description"] = (object?)null };
                    var dt = await _db.ExecuteQueryAsync(insertTableSql, param).ConfigureAwait(false);
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
                var columns = await _db.ExecuteQueryAsync(columnsQuery, new Dictionary<string, object?> { ["@tableName"] = tableName }).ConfigureAwait(false);

                foreach (DataRow colRow in columns.Rows)
                {
                    var colName = colRow["COLUMN_NAME"].ToString()!;
                    var dataType = colRow["DATA_TYPE"].ToString()!;
                    var maxLengthObj = colRow["CHARACTER_MAXIMUM_LENGTH"];
                    var maxLength = maxLengthObj != DBNull.Value ? Convert.ToInt32(maxLengthObj) : 0;
                    var isNullable = string.Equals(colRow["IS_NULLABLE"].ToString(), "YES", StringComparison.OrdinalIgnoreCase);

                    const string checkColumnSql = "SELECT Id FROM SField WHERE TableId = @tableId AND LIBELLE = @colName";
                    var exists = await _db.ExecuteQueryAsync(checkColumnSql, new Dictionary<string, object?>
                    {
                        ["@tableId"] = tableId,
                        ["@colName"] = colName
                    }).ConfigureAwait(false);
                    if (exists.Rows.Count == 0)
                    {
                        // Insert column metadata.  We assume no primary key or unique constraints for discovered columns.
                        const string insertColSql = @"
                            INSERT INTO S_CHAMP (
                                TableId, Libelle, Alias, DataType,
                                IsClePrimaire, IsUnique, Taille, Referentiel,
                                ReferentielWhereClause, Defaut, IsNulleable,
                                )
                            VALUES (
                                @tableId, @libelle, @alias, @dataType,
                                0, 0, @taille, NULL,
                                NULL, NULL, @isNullable
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
                        await _db.ExecuteQueryAsync(insertColSql, colParams).ConfigureAwait(false);
                    }
                }
            }

            // Invalidate cached metadata.
            await _cache.SetAsync(TablesCacheKey, (IList<STable>?)null!, TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }


        /// <summary>
        /// Catalogs the database schema into the STable and SField metadata tables.
        /// </summary>
        /// <returns></returns>
        public async Task SynchronizeCatalogAsync()
        {
            // Retrieve all user tables from the database excluding system tables and the catalog itself.
            const string tablesQuery = @"
                SELECT name
                FROM sys.tables
                WHERE type = 'U' AND name NOT LIKE 'S_%'";
            var tables = await _db.ExecuteQueryAsync(tablesQuery).ConfigureAwait(false);

            foreach (DataRow row in tables.Rows)
            {
                var tableName = row["name"].ToString()!;

                // Insert table metadata into STable if it does not already exist.
                // Use an upsert pattern: first check for existence.
                const string checkTableExistsSql = "SELECT Id FROM STable WHERE Libelle = @name";
                var existing = await _db.ExecuteQueryAsync(checkTableExistsSql, new Dictionary<string, object?> { ["@name"] = tableName }).ConfigureAwait(false);
                int tableId;
                if (existing.Rows.Count == 0)
                {
                    const string insertTableSql = @"
                        INSERT INTO STable (
                            Libelle, Description, Parent, ParentLiaison, ReferentielLibelle, Type)
                        VALUES (@libelle, @description, NULL, NULL, NULL, 'F')
                        );
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    var param = new Dictionary<string, object?> { ["@libelle"] = tableName, ["@description"] = (object?)null };
                    var dt = await _db.ExecuteQueryAsync(insertTableSql, param).ConfigureAwait(false);
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
                var columns = await _db.ExecuteQueryAsync(columnsQuery, new Dictionary<string, object?> { ["@tableName"] = tableName }).ConfigureAwait(false);

                foreach (DataRow colRow in columns.Rows)
                {
                    var colName = colRow["COLUMN_NAME"].ToString()!;
                    var dataType = colRow["DATA_TYPE"].ToString()!;
                    var maxLengthObj = colRow["CHARACTER_MAXIMUM_LENGTH"];
                    var maxLength = maxLengthObj != DBNull.Value ? Convert.ToInt32(maxLengthObj) : 0;
                    var isNullable = string.Equals(colRow["IS_NULLABLE"].ToString(), "YES", StringComparison.OrdinalIgnoreCase);

                    const string checkColumnSql = "SELECT Id FROM SField WHERE TableId = @tableId AND LIBELLE = @colName";
                    var exists = await _db.ExecuteQueryAsync(checkColumnSql, new Dictionary<string, object?>
                    {
                        ["@tableId"] = tableId,
                        ["@colName"] = colName
                    }).ConfigureAwait(false);
                    if (exists.Rows.Count == 0)
                    {
                        // Insert column metadata.  We assume no primary key or unique constraints for discovered columns.
                        const string insertColSql = @"
                            INSERT INTO S_CHAMP (
                                TableId, Libelle, Alias, DataType,
                                IsClePrimaire, IsUnique, Taille, Referentiel,
                                ReferentielWhereClause, Defaut, IsNulleable,
                                )
                            VALUES (
                                @tableId, @libelle, @alias, @dataType,
                                0, 0, @taille, NULL,
                                NULL, NULL, @isNullable
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
                        await _db.ExecuteQueryAsync(insertColSql, colParams).ConfigureAwait(false);
                    }
                }
            }

            // Invalidate cached metadata.
            await _cache.SetAsync(TablesCacheKey, (IList<STable>?)null!, TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronises the physical database by creating the specified table with its fields.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task CreatePhysicalTableAsync(STable table, IEnumerable<SField> fields)
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
            await _db.ExecuteNonQueryAsync(createSql).ConfigureAwait(false);
        }

        /// <summary>
        /// Retourne toutes les lignes d'une table donnée. 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<DataTable> GetAllAsync(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            var sql = $"SELECT * FROM [{tableName}]";
            return await _db.ExecuteQueryAsync(sql).ConfigureAwait(false);
        }

        /// <summary>
        /// Retroune l'Id d'une ligne donnée d'une table donnée.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="primaryKeyName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<IDictionary<string, object?>> GetByIdAsync(string tableName, string primaryKeyName, object id)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (string.IsNullOrWhiteSpace(primaryKeyName)) throw new ArgumentNullException(nameof(primaryKeyName));
            var sql = $"SELECT * FROM [{tableName}] WHERE [{primaryKeyName}] = @id";
            var result = await _db.ExecuteQueryAsync(sql, new Dictionary<string, object?> { ["@id"] = id }).ConfigureAwait(false);
            if (result.Rows.Count == 0) return new Dictionary<string, object?>();
            return result.Rows[0].Table.Columns.Cast<DataColumn>().ToDictionary(col => col.ColumnName, col => result.Rows[0][col]);
        }

        /// <summary>
        /// Insertion d'une nouvelle ligne dans une table donnée.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public async Task<int> InsertAsync(string tableName, IDictionary<string, object?> values)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (values == null || values.Count == 0) throw new ArgumentException("Values cannot be null or empty", nameof(values));
            var columns = values.Keys.ToArray();
            var parameters = columns.Select(c => "@" + c).ToArray();
            var sql = $"INSERT INTO [{tableName}] ({string.Join(", ", columns.Select(c => $"[{c}]"))}) VALUES ({string.Join(", ", parameters)})";
            var paramDict = values.ToDictionary(kvp => "@" + kvp.Key, kvp => kvp.Value);
            var affected = await _db.ExecuteNonQueryAsync(sql, paramDict).ConfigureAwait(false);
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

        /// <summary>
        /// Mise à jour d'une ligne existante dans une table donnée.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="primaryKeyName"></param>
        /// <param name="id"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
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
            IList<SField>? fieldsMetaForHistory = null;
            if (tableMeta?.IsHistorise == true)
            {
                fieldsMetaForHistory = await GetFieldsMetadataAsync(tableMeta.Id).ConfigureAwait(false);
                existingRow = await GetByIdAsync(tableName, primaryKeyName, id).ConfigureAwait(false);
            }
            // Execute the update
            var affected = await _db.ExecuteNonQueryAsync(sql, paramDict).ConfigureAwait(false);
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

        /// <summary>
        /// Suppression d'une ligne dans une table donnée.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="primaryKeyName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<int> DeleteAsync(string tableName, string primaryKeyName, object id)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (string.IsNullOrWhiteSpace(primaryKeyName)) throw new ArgumentNullException(nameof(primaryKeyName));
            var sql = $"DELETE FROM [{tableName}] WHERE [{primaryKeyName}] = @id";
            // Determine if historisation is enabled
            var tableMeta = await GetTableMetadataAsync(tableName).ConfigureAwait(false);
            IDictionary<string, object?>? existingRow = null;
            IList<SField>? fieldsMetaForHistory = null;
            if (tableMeta?.IsHistorise == true)
            {
                fieldsMetaForHistory = await GetFieldsMetadataAsync(tableMeta.Id).ConfigureAwait(false);
                // Fetch existing values before deletion only if history is enabled
                existingRow = await GetByIdAsync(tableName, primaryKeyName, id).ConfigureAwait(false);
            }
            // Perform the deletion
            var affected = await _db.ExecuteNonQueryAsync(sql, new Dictionary<string, object?> { ["@id"] = id }).ConfigureAwait(false);
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

        /// <summary>
        /// Restauration d'une ligne supprimée dans une table donnée.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<int> RestoreAsync(string tableName, int id)
        {
            var sql = $@"UPDATE dbo.{tableName}
                         SET Deleted=0, DtSuppression=NULL, UsrSuppressionId=NULL
                         WHERE ID=@ID;";
            return await _db.ExecuteNonQueryAsync(sql, new Dictionary<string, object?> { ["@ID"] = id });
        }

        private static Dictionary<string, object?> ToParams(IDictionary<string, object?> src)
        {
            var dict = new Dictionary<string, object?>(src.Count);
            foreach (var kv in src)
                dict[$"@{kv.Key}"] = kv.Value;
            return dict;
        }

        #region Helpers

        /// <summary>
        /// Builds the SQL data type clause for a column.
        /// Handles length for character types and leaves other types unchanged.
        /// </summary>
        private static string BuildSqlDataType(SField field)
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
        private static string BuildNullability(SField field) => field.IsNulleable ? "NULL" : "NOT NULL";

        /// <summary>
        /// Formats a default value for inclusion in an SQL statement.
        /// Detects string values and wraps them in quotes.
        /// </summary>
        private static string FormatDefaultValue(SField field)
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
        /// Retrieves metadata for a table (STable) by its physical name.  Uses
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
            // Query STable to obtain the table definition
            const string sql = "SELECT Id, LIBELLE, DESCRIPTION, PARENT, PARENTLIAISON, REFERENTIELLIBELLE, TYPE, DOC, ACTIF, DELETED, IS_HISTORISE FROM S_TABLE WHERE LIBELLE = @name";
            var dt = await _db.ExecuteQueryAsync(sql, new Dictionary<string, object?> { ["@name"] = tableName }).ConfigureAwait(false);
            if (dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            var table = new STable
            {
                Id = Convert.ToInt32(row["Id"]),
                Libelle = row["LIBELLE"].ToString()!,
                Description = row["DESCRIPTION"] as string,
                Parent = row["PARENT"] as string,
                ParentLiaison = row["PARENTLIAISON"] as string,
                ReferentielLibelle = row["REFERENTIELLIBELLE"] as string,
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
        private async Task<IList<SField>> GetFieldsMetadataAsync(int tableId)
        {
            var cacheKey = ChampsCacheKeyPrefix + tableId;
            var cached = await _cache.GetAsync<IList<SField>>(cacheKey).ConfigureAwait(false);
            if (cached != null)
            {
                return cached;
            }
            const string sql = @"
                SELECT Id, TABLEID, LIBELLE, ALIAS, DATATYPE, ISCLEPRIMAIRE, ISUNIQUE, TAILLE,
                       REFERENTIEL, REFERENTIELWHERECLAUSE, DEFAUT, ISNULLEABLE,
                       MIN, MAX, REGEX, ISVISIBLE, ORDRE, FORMAT, MASQUE,
                       ISLINKTOBDD, ISSEARCH, SEARCHOPERATOR, SEARCHDEFAUTVALUE,
                       COORDONNEEX, COORDONNEEY, COORDONNEELABELX, COORDONNEELABELY, COMMENTAIRE,
                       DOC, ACTIF, DELETED, ISHISTORISE,
                       SCRIPTVALIDATION, YAMLVALIDATION
                FROM SField
                WHERE TABLE_ID = @tableId";
            var dt = await _db.ExecuteQueryAsync(sql, new Dictionary<string, object?> { ["@tableId"] = tableId }).ConfigureAwait(false);
            var champs = new List<SField>(dt.Rows.Count);
            foreach (DataRow row in dt.Rows)
            {
                champs.Add(new SField
                {
                    Id = Convert.ToInt32(row["Id"]),
                    TableId = Convert.ToInt32(row["TABLEID"]),
                    Libelle = row["LIBELLE"].ToString()!,
                    Alias = row["ALIAS"] as string,
                    DataType = row["DATATYPE"].ToString()!,
                    IsClePrimaire = Convert.ToBoolean(row["ISCLEPRIMAIRE"]),
                    IsUnique = Convert.ToBoolean(row["ISUNIQUE"]),
                    Taille = Convert.ToInt32(row["TAILLE"]),
                    Referentiel = row["REFERENTIEL"] as string,
                    ReferentielWhereClause = row["REFERENTIELWHERECLAUSE"] as string,
                    Defaut = row["DEFAUT"] as string,
                    IsNulleable = Convert.ToBoolean(row["ISNULLEABLE"]),
                    Min = row["MIN"] as string,
                    Max = row["MAX"] as string,
                    Regex = row["REGEX"] as string,
                    IsVisible = Convert.ToBoolean(row["ISVISIBLE"]),
                    Ordre = row["ORDRE"] != DBNull.Value ? (int?)Convert.ToInt32(row["ORDRE"]) : null,
                    Format = row["FORMAT"] as string,
                    Masque = row["MASQUE"] as string,
                    IsLinkToBdd = Convert.ToBoolean(row["ISLINKTOBDD"]),
                    IsSearch = Convert.ToBoolean(row["ISSEARCH"]),
                    SearchOperator = row["SEARCHOPERATOR"] as string,
                    SearchDefautValue = row["SEARCHDEFAUTVALUE"] as string,
                    CoordonneeX = Convert.ToInt32(row["COORDONNEEX"]),
                    CoordonneeY = Convert.ToInt32(row["COORDONNEEY"]),
                    CoordonneeLabelX = Convert.ToInt32(row["COORDONNEELABELX"]),
                    CoordonneeLabelY = Convert.ToInt32(row["COORDONNEELABELY"]),
                    Commentaire = row["COMMENTAIRE"] as string,
                    Doc = Convert.ToBoolean(row["DOC"]),
                    Actif = Convert.ToBoolean(row["ACTIF"]),
                    Deleted = Convert.ToBoolean(row["DELETED"]),
                    IsHistorise = row.Table.Columns.Contains("ISHISTORISE") && Convert.ToBoolean(row["ISHISTORISE"]),
                    ValidationScript = row.Table.Columns.Contains("SCRIPTVALIDATION") ? row["SCRIPTVALIDATION"] as string : null,
                    ValidationYaml = row.Table.Columns.Contains("YAMLVALIDATION") ? row["YAMLVALIDATION"] as string : null
                });
            }
            await _cache.SetAsync(cacheKey, champs, TimeSpan.FromMinutes(5)).ConfigureAwait(false);
            return champs;
        }
        #endregion
    }
}
