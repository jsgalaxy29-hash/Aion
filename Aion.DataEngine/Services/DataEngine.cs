using Aion.DataEngine.Entities;
using Aion.DataEngine.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Aion.Infrastructure.Data;
using System.Text;
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
            var tableNames = await GetUserTablesAsync(tablename).ConfigureAwait(false);

            foreach (var tableName in tableNames)
            {
                var tableId = await EnsureTableMetadataAsync(tableName).ConfigureAwait(false);
                var columns = await GetColumnMetadataAsync(tableName).ConfigureAwait(false);

                foreach (var column in columns)
                {
                    await EnsureColumnMetadataAsync(tableId, column).ConfigureAwait(false);
                }
            }

            await _cache.RemoveAsync(TablesCacheKey).ConfigureAwait(false);
        }




        private async Task<IReadOnlyList<string>> GetUserTablesAsync(string? tableFilter)
        {
            IDictionary<string, object?>? parameters = null;
            string sql;
            if (_db is SqliteDataProvider)
            {
                sql = "SELECT name FROM sqlite_schema WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";
                if (!string.IsNullOrWhiteSpace(tableFilter))
                {
                    sql += " AND name = @tableName";
                    parameters = new Dictionary<string, object?> { ["@tableName"] = tableFilter };
                }
            }
            else
            {
                sql = "SELECT name FROM sys.tables WHERE type = 'U'";
                if (!string.IsNullOrWhiteSpace(tableFilter))
                {
                    sql += " AND name = @tableName";
                    parameters = new Dictionary<string, object?> { ["@tableName"] = tableFilter };
                }
            }

            var result = await _db.ExecuteQueryAsync(sql, parameters).ConfigureAwait(false);
            return result.Rows.Cast<DataRow>()
                .Select(row => row["name"].ToString()!)
                .ToArray();
        }

        private async Task<int> EnsureTableMetadataAsync(string tableName)
        {
            const string checkSql = "SELECT Id FROM STable WHERE Libelle = @name";
            var existing = await _db.ExecuteQueryAsync(checkSql, new Dictionary<string, object?>
            {
                ["@name"] = tableName
            }).ConfigureAwait(false);

            if (existing.Rows.Count > 0)
            {
                return Convert.ToInt32(existing.Rows[0][0], CultureInfo.InvariantCulture);
            }

            var insertSql = _db is SqliteDataProvider
                ? @"INSERT INTO STable (Libelle, Description, Parent, ParentLiaison, ReferentielLibelle, Type)
                    VALUES (@libelle, @description, NULL, NULL, NULL, 'F');
                    SELECT last_insert_rowid();"
                : @"INSERT INTO STable (Libelle, Description, Parent, ParentLiaison, ReferentielLibelle, Type)
                    VALUES (@libelle, @description, NULL, NULL, NULL, 'F');
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var insertedId = await _db.ExecuteScalarAsync(insertSql, new Dictionary<string, object?>
            {
                ["@libelle"] = tableName,
                ["@description"] = (object?)null
            }).ConfigureAwait(false);

            return Convert.ToInt32(insertedId ?? throw new InvalidOperationException("Unable to capture STable identifier"), CultureInfo.InvariantCulture);
        }

        private async Task<IReadOnlyList<ColumnMetadata>> GetColumnMetadataAsync(string tableName)
        {
            if (_db is SqliteDataProvider)
            {
                return await GetSqliteColumnMetadataAsync(tableName).ConfigureAwait(false);
            }

            return await GetSqlServerColumnMetadataAsync(tableName).ConfigureAwait(false);
        }

        private async Task EnsureColumnMetadataAsync(int tableId, ColumnMetadata column)
        {
            const string checkSql = "SELECT Id FROM SField WHERE TableId = @tableId AND LIBELLE = @colName";
            var exists = await _db.ExecuteQueryAsync(checkSql, new Dictionary<string, object?>
            {
                ["@tableId"] = tableId,
                ["@colName"] = column.Name
            }).ConfigureAwait(false);

            if (exists.Rows.Count > 0)
            {
                return;
            }

            var insertSql = _db is SqliteDataProvider
                ? @"INSERT INTO SField (
                        TableId, Libelle, Alias, DataType,
                        IsClePrimaire, IsUnique, Taille, Referentiel,
                        Defaut, IsNulleable)
                    VALUES (
                        @tableId, @libelle, @alias, @dataType,
                        @IsPrimaryKey, @IsUnique, @taille, @FKTable,
                        @defaultvalue, @isNullable);
                    SELECT last_insert_rowid();"
                : @"INSERT INTO SField (
                        TableId, Libelle, Alias, DataType,
                        IsClePrimaire, IsUnique, Taille, Referentiel,
                        Defaut, IsNulleable)
                    VALUES (
                        @tableId, @libelle, @alias, @dataType,
                        @IsPrimaryKey, @IsUnique, @taille, @FKTable,
                        @defaultvalue, @isNullable);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

            await _db.ExecuteScalarAsync(insertSql, new Dictionary<string, object?>
            {
                ["@tableId"] = tableId,
                ["@libelle"] = column.Name,
                ["@alias"] = column.Name,
                ["@dataType"] = column.DataType,
                ["@FKTable"] = column.ForeignTable,
                ["@taille"] = column.Size ?? 0,
                ["@isNullable"] = column.IsNullable,
                ["@IsPrimaryKey"] = column.IsPrimaryKey,
                ["@IsUnique"] = column.IsUnique,
                ["@defaultvalue"] = column.DefaultValue
            }).ConfigureAwait(false);
        }

        private async Task<IReadOnlyList<ColumnMetadata>> GetSqlServerColumnMetadataAsync(string tableName)
        {
            const string query = @"
                SELECT
                    c.COLUMN_NAME,
                    c.COLUMN_DEFAULT,
                    c.IS_NULLABLE,
                    c.DATA_TYPE,
                    c.CHARACTER_MAXIMUM_LENGTH,
                    c.NUMERIC_PRECISION,
                    c.NUMERIC_SCALE,
                    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 'Oui' ELSE 'Non' END AS IsPrimaryKey,
                    CASE WHEN uq.COLUMN_NAME IS NOT NULL THEN 'Oui' ELSE 'Non' END AS IsUnique,
                    fk.ReferencedTableName AS FKTable
                FROM INFORMATION_SCHEMA.COLUMNS c
                LEFT JOIN (
                    SELECT ku.TABLE_NAME, ku.COLUMN_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                        ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                ) pk ON c.TABLE_NAME = pk.TABLE_NAME AND c.COLUMN_NAME = pk.COLUMN_NAME
                LEFT JOIN (
                    SELECT ku.TABLE_NAME, ku.COLUMN_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                        ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                    WHERE tc.CONSTRAINT_TYPE = 'UNIQUE'
                ) uq ON c.TABLE_NAME = uq.TABLE_NAME AND c.COLUMN_NAME = uq.COLUMN_NAME
                LEFT JOIN (
                    SELECT
                        OBJECT_NAME(fk.parent_object_id)            AS TableName,
                        pc.name                                     AS ColumnName,
                        OBJECT_NAME(fk.referenced_object_id)        AS ReferencedTableName
                    FROM sys.foreign_keys fk
                    JOIN sys.foreign_key_columns fkc
                        ON fk.object_id = fkc.constraint_object_id
                    JOIN sys.columns pc
                        ON fkc.parent_object_id = pc.object_id
                       AND fkc.parent_column_id = pc.column_id
                ) fk ON c.TABLE_NAME = fk.TableName AND c.COLUMN_NAME = fk.ColumnName
                WHERE c.TABLE_NAME = @tableName
                ORDER BY c.ORDINAL_POSITION;";

            var rows = await _db.ExecuteQueryAsync(query, new Dictionary<string, object?>
            {
                ["@tableName"] = tableName
            }).ConfigureAwait(false);

            var result = new List<ColumnMetadata>(rows.Rows.Count);
            foreach (DataRow row in rows.Rows)
            {
                var dataType = row["DATA_TYPE"].ToString() ?? string.Empty;
                var maxLength = row["CHARACTER_MAXIMUM_LENGTH"] == DBNull.Value
                    ? (int?)null
                    : Convert.ToInt32(row["CHARACTER_MAXIMUM_LENGTH"], CultureInfo.InvariantCulture);

                if (maxLength == -1)
                {
                    maxLength = 9999;
                }
                else if ((maxLength == null || maxLength == 0) && !string.IsNullOrEmpty(dataType))
                {
                    switch (dataType)
                    {
                        case "int":
                        case "smallint":
                        case "float":
                            var precisionValue = row["NUMERIC_PRECISION"];
                            if (precisionValue != null && precisionValue != DBNull.Value)
                            {
                                maxLength = Convert.ToInt32(precisionValue, CultureInfo.InvariantCulture);
                            }
                            break;
                    }
                }

                var fkValue = row["FKTable"];
                var defaultValue = row["COLUMN_DEFAULT"];

                result.Add(new ColumnMetadata(
                    row["COLUMN_NAME"].ToString()!,
                    dataType,
                    string.Equals(row["IS_NULLABLE"].ToString(), "YES", StringComparison.OrdinalIgnoreCase),
                    string.Equals(row["IsPrimaryKey"].ToString(), "Oui", StringComparison.OrdinalIgnoreCase),
                    string.Equals(row["IsUnique"].ToString(), "Oui", StringComparison.OrdinalIgnoreCase),
                    fkValue == null || fkValue == DBNull.Value ? null : fkValue.ToString(),
                    maxLength,
                    defaultValue == null || defaultValue == DBNull.Value ? null : defaultValue.ToString()
                ));
            }

            return result;
        }

        private async Task<IReadOnlyList<ColumnMetadata>> GetSqliteColumnMetadataAsync(string tableName)
        {
            var quotedTable = QuoteIdentifier(tableName, isSqlite: true);
            var tableInfo = await _db.ExecuteQueryAsync($"PRAGMA table_info({quotedTable});").ConfigureAwait(false);
            var uniqueColumns = await GetSqliteUniqueColumnsAsync(tableName).ConfigureAwait(false);
            var foreignKeys = await GetSqliteForeignKeysAsync(tableName).ConfigureAwait(false);

            var result = new List<ColumnMetadata>(tableInfo.Rows.Count);
            foreach (DataRow row in tableInfo.Rows)
            {
                var columnName = row["name"]?.ToString();
                if (string.IsNullOrWhiteSpace(columnName))
                {
                    continue;
                }

                var dataType = row["type"]?.ToString() ?? "TEXT";
                var defaultValue = row["dflt_value"] is DBNull ? null : row["dflt_value"]?.ToString();
                var isPrimaryKey = IsTruthy(row["pk"]);
                var isUnique = isPrimaryKey || uniqueColumns.Contains(columnName);
                foreignKeys.TryGetValue(columnName, out var fkTable);

                result.Add(new ColumnMetadata(
                    columnName,
                    dataType,
                    !IsTruthy(row["notnull"]),
                    isPrimaryKey,
                    isUnique,
                    fkTable,
                    TryParseSqliteLength(dataType),
                    defaultValue
                ));
            }

            return result;
        }

        private async Task<HashSet<string>> GetSqliteUniqueColumnsAsync(string tableName)
        {
            var quotedTable = QuoteIdentifier(tableName, isSqlite: true);
            var indexList = await _db.ExecuteQueryAsync($"PRAGMA index_list({quotedTable});").ConfigureAwait(false);
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow index in indexList.Rows)
            {
                if (!IsTruthy(index["unique"]))
                {
                    continue;
                }

                var indexName = index["name"]?.ToString();
                if (string.IsNullOrWhiteSpace(indexName))
                {
                    continue;
                }

                var quotedIndex = QuoteIdentifier(indexName, isSqlite: true);
                var indexInfo = await _db.ExecuteQueryAsync($"PRAGMA index_info({quotedIndex});").ConfigureAwait(false);
                foreach (DataRow info in indexInfo.Rows)
                {
                    var columnName = info["name"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(columnName))
                    {
                        result.Add(columnName);
                    }
                }
            }

            return result;
        }

        private async Task<Dictionary<string, string>> GetSqliteForeignKeysAsync(string tableName)
        {
            var quotedTable = QuoteIdentifier(tableName, isSqlite: true);
            var fkInfo = await _db.ExecuteQueryAsync($"PRAGMA foreign_key_list({quotedTable});").ConfigureAwait(false);
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow row in fkInfo.Rows)
            {
                var column = row["from"]?.ToString();
                var referencedTable = row["table"]?.ToString();
                if (!string.IsNullOrWhiteSpace(column) && !string.IsNullOrWhiteSpace(referencedTable))
                {
                    result[column] = referencedTable;
                }
            }

            return result;
        }

        private static int? TryParseSqliteLength(string dataType)
        {
            if (string.IsNullOrWhiteSpace(dataType))
            {
                return null;
            }

            var openParen = dataType.IndexOf('(');
            var closeParen = dataType.IndexOf(')', openParen + 1);
            if (openParen < 0 || closeParen <= openParen)
            {
                return null;
            }

            var lengthPart = dataType.Substring(openParen + 1, closeParen - openParen - 1);
            var commaIndex = lengthPart.IndexOf(',');
            if (commaIndex >= 0)
            {
                lengthPart = lengthPart[..commaIndex];
            }

            return int.TryParse(lengthPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : null;
        }

        private static bool IsTruthy(object? value)
        {
            return value switch
            {
                null => false,
                bool b => b,
                sbyte sb => sb != 0,
                byte b8 => b8 != 0,
                short s => s != 0,
                ushort us => us != 0,
                int i => i != 0,
                uint ui => ui != 0,
                long l => l != 0,
                ulong ul => ul != 0,
                string s => s.Equals("1", StringComparison.OrdinalIgnoreCase) || s.Equals("true", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        private static string QuoteIdentifier(string identifier, bool isSqlite)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return identifier;
            }

            return isSqlite
                ? "\"" + identifier.Replace("\"", "\"\"") + "\""
                : "[" + identifier.Replace("]", "]]") + "]";
        }

        private sealed record ColumnMetadata(
            string Name,
            string DataType,
            bool IsNullable,
            bool IsPrimaryKey,
            bool IsUnique,
            string? ForeignTable,
            int? Size,
            string? DefaultValue);

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
