using Aion.DataEngine.Entities;
using Aion.DataEngine.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

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

        public DataEngine(IDataProvider db, IUserContext user, IClock clock)
            : this(db,
                  user,
                  clock,
                  new SimpleValidationService(),
                  new NoOpHistorizationService(),
                  new MemoryCacheService(new MemoryCache(new MemoryCacheOptions())))
        {
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
        public async Task SynchronizeCatalogAsync(string? tablename = null)
        {
            // Retrieve all user tables from the database excluding system tables and the catalog itself.
            string tablesQuery = @"
                SELECT name
                FROM sys.tables
                WHERE type = 'U'";

            IDictionary<string, object?>? tableParams = null;
            if (!string.IsNullOrWhiteSpace(tablename))
            {
                tablesQuery += " AND name = @tableName";
                tableParams = new Dictionary<string, object?> { ["@tableName"] = tablename };
            }

            var tables = await _db.ExecuteQueryAsync(tablesQuery, tableParams).ConfigureAwait(false);

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
                        VALUES (@libelle, @description, NULL, NULL, NULL, 'F');
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
                                           SELECT 
                                            c.TABLE_SCHEMA,
                                            c.TABLE_NAME,
                                            c.COLUMN_NAME,
                                            c.ORDINAL_POSITION,
                                            c.COLUMN_DEFAULT,
                                            c.IS_NULLABLE,
                                            c.DATA_TYPE,
                                            c.CHARACTER_MAXIMUM_LENGTH,
                                            c.NUMERIC_PRECISION,
                                            c.NUMERIC_SCALE,

                                            -- Clé primaire ?
                                            CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 'Oui' ELSE 'Non' END AS IsPrimaryKey,

                                            -- Contrainte UNIQUE ?
                                            CASE WHEN uq.COLUMN_NAME IS NOT NULL THEN 'Oui' ELSE 'Non' END AS IsUnique,

                                            -- Table référencée par la FK (uniquement si le nom de colonne finit par 'Id')
                                            CASE 
                                                WHEN c.COLUMN_NAME LIKE '%Id' THEN fk.ReferencedTableName
                                                ELSE NULL
                                            END AS FKTable

                                        FROM INFORMATION_SCHEMA.COLUMNS c

                                        -- PK
                                        LEFT JOIN (
                                            SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
                                            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                                            JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                                                ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                                               AND tc.TABLE_SCHEMA   = ku.TABLE_SCHEMA
                                            WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                                        ) pk ON  c.TABLE_SCHEMA = pk.TABLE_SCHEMA
                                           AND c.TABLE_NAME   = pk.TABLE_NAME
                                           AND c.COLUMN_NAME  = pk.COLUMN_NAME

                                        -- UNIQUE
                                        LEFT JOIN (
                                            SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
                                            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                                            JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                                                ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                                               AND tc.TABLE_SCHEMA   = ku.TABLE_SCHEMA
                                            WHERE tc.CONSTRAINT_TYPE = 'UNIQUE'
                                        ) uq ON  c.TABLE_SCHEMA = uq.TABLE_SCHEMA
                                           AND c.TABLE_NAME   = uq.TABLE_NAME
                                           AND c.COLUMN_NAME  = uq.COLUMN_NAME

                                        -- FK → table référencée
                                        LEFT JOIN (
                                            SELECT 
                                                OBJECT_SCHEMA_NAME(fk.parent_object_id)     AS SchemaName,
                                                OBJECT_NAME(fk.parent_object_id)            AS TableName,
                                                pc.name                                     AS ColumnName,
                                                OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS ReferencedSchemaName,
                                                OBJECT_NAME(fk.referenced_object_id)        AS ReferencedTableName
                                            FROM sys.foreign_keys fk
                                            JOIN sys.foreign_key_columns fkc 
                                                ON fk.object_id = fkc.constraint_object_id
                                            JOIN sys.columns pc
                                                ON fkc.parent_object_id = pc.object_id 
                                               AND fkc.parent_column_id = pc.column_id
                                        ) fk ON  c.TABLE_SCHEMA = fk.SchemaName
                                            AND c.TABLE_NAME   = fk.TableName
                                            AND c.COLUMN_NAME  = fk.ColumnName

                                        WHERE c.TABLE_NAME = @tableName
                                        ORDER BY c.ORDINAL_POSITION;";
                var columns = await _db.ExecuteQueryAsync(columnsQuery, new Dictionary<string, object?> { ["@tableName"] = tableName }).ConfigureAwait(false);

                foreach (DataRow colRow in columns.Rows)
                {
                    var colName = colRow["COLUMN_NAME"].ToString()!;
                    var dataType = colRow["DATA_TYPE"].ToString()!;
                    var maxLengthObj = colRow["CHARACTER_MAXIMUM_LENGTH"];

                    var maxLength = maxLengthObj != DBNull.Value ? Convert.ToInt32(maxLengthObj) : 0;
                    if (maxLength == 0)
                    {
                        if (dataType != null)
                        {
                            switch (dataType)
                            {
                                case "int":
                                case "smallint":
                                case "float":
                                    maxLength = int.Parse(colRow["NUMERIC_PRECISION"].ToString()!);
                                    break;

                            }
                        }
                    }
                    else if (maxLength == -1)
                    {
                        maxLength = 9999;
                    }
                    var defaultvalue = colRow["COLUMN_DEFAULT"].ToString()!;

                    var isNullable = string.Equals(colRow["IS_NULLABLE"].ToString(), "YES", StringComparison.OrdinalIgnoreCase);
                    var isPrimaryKey = string.Equals(colRow["IsPrimaryKey"].ToString(), "Oui", StringComparison.OrdinalIgnoreCase);
                    var isUnique = string.Equals(colRow["IsUnique"].ToString(), "Oui", StringComparison.OrdinalIgnoreCase);
                    var FKTable = colRow["FKTable"].ToString()!;

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
                            INSERT INTO SFIELD (
                                TableId, Libelle, Alias, DataType,
                                IsClePrimaire, IsUnique, Taille, Referentiel,
                                Defaut, IsNulleable
                                )
                            VALUES (
                                @tableId, @libelle, @alias, @dataType,
                                @IsPrimaryKey, @IsUnique, @taille, @FKTable,
                                @defaultvalue, @isNullable
                            );
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";
                        var colParams = new Dictionary<string, object?>
                        {
                            ["@tableId"] = tableId,
                            ["@libelle"] = colName,
                            ["@alias"] = colName,
                            ["@dataType"] = dataType,
                            ["@FKTable"] = string.IsNullOrWhiteSpace(FKTable) ? null : FKTable,
                            ["@taille"] = maxLength,
                            ["@isNullable"] = isNullable,
                            ["@IsPrimaryKey"] = isPrimaryKey,
                            ["@IsUnique"] = isUnique,
                            ["@defaultvalue"] = defaultvalue,
                        };
                        await _db.ExecuteQueryAsync(insertColSql, colParams).ConfigureAwait(false);
                    }
                }
            }

            // Invalidate cached metadata.
            await _cache.RemoveAsync(TablesCacheKey).ConfigureAwait(false);
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

            var fieldList = fields.ToList();
            var pkColumns = new List<string>();
            var columnDefinitions = new List<string>(fieldList.Count);
            var foreignKeys = new List<string>();

            foreach (var f in fieldList)
            {
                var columnBuilder = new StringBuilder();
                columnBuilder.Append($"    [{f.Libelle}] {BuildSqlDataType(f)} {BuildNullability(f)}");

                if (!string.IsNullOrWhiteSpace(f.Defaut))
                {
                    columnBuilder.Append($" DEFAULT {FormatDefaultValue(f)}");
                }

                if (f.IsUnique && !f.IsClePrimaire)
                {
                    columnBuilder.Append(" UNIQUE");
                }

                columnDefinitions.Add(columnBuilder.ToString());

                if (f.IsClePrimaire)
                {
                    pkColumns.Add(f.Libelle);
                }

                if (!string.IsNullOrWhiteSpace(f.Referentiel))
                {
                    foreignKeys.Add($"    CONSTRAINT FK_{table.Libelle}_{f.Libelle} FOREIGN KEY ([{f.Libelle}]) REFERENCES [{f.Referentiel}]([Id])");
                }
            }

            if (pkColumns.Count > 0)
            {
                foreignKeys.Insert(0, $"    CONSTRAINT PK_{table.Libelle} PRIMARY KEY ({string.Join(", ", pkColumns.Select(c => $"[{c}]"))})");
            }

            var allDefinitions = columnDefinitions.Concat(foreignKeys);
            builder.AppendLine(string.Join(",\n", allDefinitions));
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

        public Task InvalidateMetadataAsync(int tableId, string? tableName = null)
        {
            if (_cache == null)
            {
                return Task.CompletedTask;
            }

            var tasks = new List<Task>
            {
                _cache.RemoveAsync(TablesCacheKey)
            };

            if (!string.IsNullOrWhiteSpace(tableName))
            {
                tasks.Add(_cache.RemoveAsync($"{TablesCacheKey}:{tableName}"));
            }

            if (tableId > 0)
            {
                tasks.Add(_cache.RemoveAsync(ChampsCacheKeyPrefix + tableId));
            }

            return Task.WhenAll(tasks);
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
        public async Task<STable?> GetTableMetadataAsync(string tableName)
        {
            var cacheKey = $"{TablesCacheKey}:{tableName}";
            var cached = await _cache.GetAsync<STable>(cacheKey).ConfigureAwait(false);
            if (cached != null)
            {
                return cached;
            }
            // Query STable to obtain the table definition
            const string sql = "SELECT Id, LIBELLE, DESCRIPTION, PARENT, PARENTLIAISON, REFERENTIELLIBELLE, TYPE, DOC, ACTIF, DELETED, ISHISTORISE FROM STable WHERE LIBELLE = @name";
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
                IsHistorise = row.Table.Columns.Contains("ISHISTORISE") && Convert.ToBoolean(row["ISHISTORISE"])
            };
            await _cache.SetAsync(cacheKey, table, TimeSpan.FromMinutes(5)).ConfigureAwait(false);
            return table;
        }

        /// <summary>
        /// Retrieves metadata for all fields (SFIELD) belonging to a table.
        /// Uses an in‑memory cache to avoid repeated lookups.
        /// </summary>
        /// <param name="tableId">Identifier of the table (S_TABLE.Id).</param>
        public async Task<IList<SField>> GetFieldsMetadataAsync(int tableId)
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
                       ValidationScript, ValidationYaml
                FROM SField
                WHERE TABLEID = @tableId";
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
                    Regex = row.Table.Columns.Contains("REGEX") ? row["REGEX"] as string : null,
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
                    ValidationScript = row.Table.Columns.Contains("ValidationScript") ? row["ValidationScript"] as string : null,
                    ValidationYaml = row.Table.Columns.Contains("ValidationYaml") ? row["ValidationYaml"] as string : null
                });
            }
            await _cache.SetAsync(cacheKey, champs, TimeSpan.FromMinutes(5)).ConfigureAwait(false);
            return champs;
        }

        public async Task<DataTable> GetReferentialAsync(string tableName, string? whereClause)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));

            var sql = $"SELECT * FROM [{tableName}]";
            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                sql += $" WHERE {whereClause}";
            }

            return await _db.ExecuteQueryAsync(sql).ConfigureAwait(false);
        }
        #endregion
    }
}
