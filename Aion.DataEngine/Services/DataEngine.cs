using Aion.DataEngine.Entities;
using Aion.DataEngine.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aion.DataEngine.Models;
using Aion.Infrastructure.Data;
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

        private const int DefaultPageSize = 50;
        private const int MaxPageSize = 500;

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

        private async Task<(STable table, IList<SField> fields)> GetMetadataAsync(string tableName)
        {
            var table = await GetTableMetadataAsync(tableName).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Table '{tableName}' not found in metadata.");

            var fields = await GetFieldsMetadataAsync(table.Id).ConfigureAwait(false);
            return (table, fields);
        }

        private static bool HasColumn(IEnumerable<SField> fields, string columnName)
            => fields.Any(f => string.Equals(f.Libelle, columnName, StringComparison.OrdinalIgnoreCase));

        private bool TryAppendTenantFilter(IList<SField> fields, ICollection<string> whereClauses, IDictionary<string, object?> parameters)
        {
            var tenantField = fields.FirstOrDefault(f => string.Equals(f.Libelle, "TenantId", StringComparison.OrdinalIgnoreCase));
            if (tenantField is null)
            {
                return false;
            }

            whereClauses.Add($"{SqlIdentifierHelper.QuoteColumn(tenantField.Libelle)} = @tenantId");
            parameters["@tenantId"] = _user.TenantId;
            return true;
        }

        private void AppendFilterClause(DataFilter filter, IList<SField> fields, ICollection<string> whereClauses, IDictionary<string, object?> parameters)
        {
            if (string.IsNullOrWhiteSpace(filter.FieldName) || string.IsNullOrWhiteSpace(filter.Value))
            {
                return;
            }

            var field = fields.FirstOrDefault(f => string.Equals(f.Libelle, filter.FieldName, StringComparison.OrdinalIgnoreCase));
            if (field is null)
            {
                return;
            }

            var columnSql = SqlIdentifierHelper.QuoteColumn(field.Libelle);
            var parameterName = $"@p{parameters.Count}";
            var op = (filter.Operator ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(op))
            {
                op = "contains";
            }

            switch (op)
            {
                case "=":
                case "eq":
                    if (TryConvertFilterValue(field, filter.Value!, out var eqValue))
                    {
                        parameters[parameterName] = eqValue;
                        if (IsStringField(field))
                        {
                            whereClauses.Add($"LOWER({columnSql}) = LOWER({parameterName})");
                        }
                        else
                        {
                            whereClauses.Add($"{columnSql} = {parameterName}");
                        }
                    }
                    break;
                case "ne":
                case "!=":
                    if (TryConvertFilterValue(field, filter.Value!, out var neValue))
                    {
                        parameters[parameterName] = neValue;
                        if (IsStringField(field))
                        {
                            whereClauses.Add($"LOWER({columnSql}) <> LOWER({parameterName})");
                        }
                        else
                        {
                            whereClauses.Add($"{columnSql} <> {parameterName}");
                        }
                    }
                    break;
                case ">":
                case "gt":
                    if (TryConvertFilterValue(field, filter.Value!, out var gtValue))
                    {
                        parameters[parameterName] = gtValue;
                        whereClauses.Add($"{columnSql} > {parameterName}");
                    }
                    break;
                case ">=":
                case "gte":
                    if (TryConvertFilterValue(field, filter.Value!, out var gteValue))
                    {
                        parameters[parameterName] = gteValue;
                        whereClauses.Add($"{columnSql} >= {parameterName}");
                    }
                    break;
                case "<":
                case "lt":
                    if (TryConvertFilterValue(field, filter.Value!, out var ltValue))
                    {
                        parameters[parameterName] = ltValue;
                        whereClauses.Add($"{columnSql} < {parameterName}");
                    }
                    break;
                case "<=":
                case "lte":
                    if (TryConvertFilterValue(field, filter.Value!, out var lteValue))
                    {
                        parameters[parameterName] = lteValue;
                        whereClauses.Add($"{columnSql} <= {parameterName}");
                    }
                    break;
                case "starts":
                    parameters[parameterName] = $"{EscapeLikeValue(filter.Value!)}%";
                    whereClauses.Add($"LOWER({columnSql}) LIKE LOWER({parameterName})");
                    break;
                case "ends":
                    parameters[parameterName] = $"%{EscapeLikeValue(filter.Value!)}";
                    whereClauses.Add($"LOWER({columnSql}) LIKE LOWER({parameterName})");
                    break;
                default:
                    parameters[parameterName] = $"%{EscapeLikeValue(filter.Value!)}%";
                    whereClauses.Add($"LOWER({columnSql}) LIKE LOWER({parameterName})");
                    break;
            }
        }

        private string BuildOrderClause(IReadOnlyList<DataSort>? sorts, IList<SField> fields, string defaultOrder)
        {
            if (sorts is null || sorts.Count == 0)
            {
                return $"ORDER BY {defaultOrder}";
            }

            var parts = new List<string>();
            foreach (var sort in sorts)
            {
                if (string.IsNullOrWhiteSpace(sort.FieldName))
                {
                    continue;
                }

                var field = fields.FirstOrDefault(f => string.Equals(f.Libelle, sort.FieldName, StringComparison.OrdinalIgnoreCase));
                if (field is null)
                {
                    continue;
                }

                var columnSql = SqlIdentifierHelper.QuoteColumn(field.Libelle);
                parts.Add($"{columnSql} {(sort.Descending ? "DESC" : "ASC")}");
            }

            if (parts.Count == 0)
            {
                return $"ORDER BY {defaultOrder}";
            }

            return $"ORDER BY {string.Join(", ", parts)}";
        }

        private string BuildPagingClause()
            => _db switch
            {
                SqliteDataProvider => "LIMIT @take OFFSET @skip",
                _ => "OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY"
            };

        private static List<Dictionary<string, object?>> ConvertToRows(DataTable data)
        {
            var result = new List<Dictionary<string, object?>>(data.Rows.Count);
            foreach (DataRow row in data.Rows)
            {
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (DataColumn column in data.Columns)
                {
                    var value = row[column];
                    dict[column.ColumnName] = value == DBNull.Value ? null : value;
                }

                result.Add(dict);
            }

            return result;
        }

        private static bool TryConvertFilterValue(SField field, string value, out object? converted)
        {
            var dataType = (field.DataType ?? string.Empty).ToLowerInvariant();

            if (dataType.Contains("int") && !dataType.Contains("point"))
            {
                if (int.TryParse(value, out var intValue))
                {
                    converted = intValue;
                    return true;
                }
            }
            else if (dataType.Contains("decimal") || dataType.Contains("numeric") || dataType.Contains("money"))
            {
                if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
                {
                    converted = decimalValue;
                    return true;
                }
            }
            else if (dataType.Contains("date") || dataType.Contains("time"))
            {
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateValue))
                {
                    converted = dateValue;
                    return true;
                }
            }
            else if (dataType.Contains("bit") || dataType.Contains("bool"))
            {
                if (bool.TryParse(value, out var boolValue))
                {
                    converted = boolValue;
                    return true;
                }
            }
            else if (dataType.Contains("uniqueidentifier"))
            {
                if (Guid.TryParse(value, out var guidValue))
                {
                    converted = guidValue;
                    return true;
                }
            }

            converted = value;
            return true;
        }

        private static bool IsStringField(SField field)
        {
            var dataType = (field.DataType ?? string.Empty).ToLowerInvariant();
            return dataType.Contains("char") || dataType.Contains("text") || dataType.Contains("nchar") || dataType.Contains("nvarchar");
        }

        private static string EscapeLikeValue(string value)
        {
            return value
                .Replace("[", "[[", StringComparison.Ordinal)
                .Replace("%", "[%]", StringComparison.Ordinal)
                .Replace("_", "[_]", StringComparison.Ordinal);
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
                    C.TABLE_NAME,
                    C.COLUMN_NAME,
                    C.COLUMN_DEFAULT,
                    C.IS_NULLABLE,
                    C.DATA_TYPE,
                    C.CHARACTER_MAXIMUM_LENGTH,
                    C.NUMERIC_PRECISION,
                    C.NUMERIC_SCALE,
                    COLUMNPROPERTY(object_id(C.TABLE_NAME), C.COLUMN_NAME, 'IsIdentity') AS IsIdentity,
                    CASE WHEN C.COLUMN_NAME IN (
                        SELECT COLUMN_NAME
                        FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                        WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_NAME), 'IsPrimaryKey') = 1
                          AND TABLE_NAME = C.TABLE_NAME
                    ) THEN 1 ELSE 0 END AS IsPrimaryKey,
                    CASE WHEN C.COLUMN_NAME IN (
                        SELECT COLUMN_NAME
                        FROM INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu
                        JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                            ON tc.CONSTRAINT_NAME = ccu.CONSTRAINT_NAME
                        WHERE tc.CONSTRAINT_TYPE = 'UNIQUE'
                          AND ccu.TABLE_NAME = C.TABLE_NAME
                    ) THEN 1 ELSE 0 END AS IsUnique,
                    (
                        SELECT FK.TABLE_NAME
                        FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC
                        JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU1 ON KCU1.CONSTRAINT_NAME = RC.CONSTRAINT_NAME
                        JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU2 ON KCU2.CONSTRAINT_NAME = RC.UNIQUE_CONSTRAINT_NAME
                        JOIN INFORMATION_SCHEMA.TABLES FK ON FK.TABLE_NAME = KCU2.TABLE_NAME
                        WHERE KCU1.COLUMN_NAME = C.COLUMN_NAME AND KCU1.TABLE_NAME = C.TABLE_NAME
                    ) AS ForeignKeyTable
                FROM INFORMATION_SCHEMA.COLUMNS C
                WHERE C.TABLE_NAME = @TableName
                ORDER BY C.ORDINAL_POSITION;";

            var rows = await _db.ExecuteQueryAsync(query, new Dictionary<string, object?>
            {
                ["@TableName"] = tableName
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

                var fkValue = row["ForeignKeyTable"];
                var defaultValue = row["COLUMN_DEFAULT"];

                result.Add(new ColumnMetadata(
                    row["COLUMN_NAME"].ToString()!,
                    dataType,
                    string.Equals(row["IS_NULLABLE"].ToString(), "YES", StringComparison.OrdinalIgnoreCase),
                    Convert.ToInt32(row["IsPrimaryKey"], CultureInfo.InvariantCulture) == 1,
                    Convert.ToInt32(row["IsUnique"], CultureInfo.InvariantCulture) == 1,
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
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException(nameof(tableName));
            }

            var (_, fields) = await GetMetadataAsync(tableName).ConfigureAwait(false);
            var selectable = fields.Where(f => f.IsLinkToBdd).ToList();
            if (selectable.Count == 0)
            {
                return new DataTable();
            }

            var columnList = string.Join(", ", selectable.Select(f => SqlIdentifierHelper.QuoteColumn(f.Libelle)));
            var tableIdentifier = SqlIdentifierHelper.QuoteTable(tableName);
            var filters = new List<string>();
            var parameters = new Dictionary<string, object?>();

            if (HasColumn(selectable, "TenantId"))
            {
                filters.Add("[TenantId] = @tenantId");
                parameters["@tenantId"] = _user.TenantId;
            }

            if (HasColumn(selectable, "Deleted"))
            {
                filters.Add("[Deleted] = 0");
            }

            var whereClause = filters.Count > 0 ? $" WHERE {string.Join(" AND ", filters)}" : string.Empty;
            var sql = $"SELECT {columnList} FROM {tableIdentifier}{whereClause}";

            return await _db.ExecuteQueryAsync(sql, parameters).ConfigureAwait(false);
        }

        public async Task<DataPage> GetPageAsync(DataPageRequest request, CancellationToken ct = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var skip = Math.Max(0, request.Skip);
            var take = request.Take <= 0 ? DefaultPageSize : Math.Min(request.Take, MaxPageSize);

            var (tableMeta, fields) = await GetMetadataAsync(request.TableName).ConfigureAwait(false);
            var selectable = fields.Where(f => f.IsLinkToBdd).ToList();
            if (selectable.Count == 0)
            {
                return new DataPage(new List<Dictionary<string, object?>>(), 0);
            }

            var tableIdentifier = SqlIdentifierHelper.QuoteTable(request.TableName);
            var columnList = string.Join(", ", selectable.Select(f => SqlIdentifierHelper.QuoteColumn(f.Libelle)));

            var primaryField = fields.FirstOrDefault(f => f.IsClePrimaire) ?? selectable.First();
            var defaultOrder = SqlIdentifierHelper.QuoteColumn(primaryField.Libelle);

            var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["@skip"] = skip,
                ["@take"] = take
            };

            var whereClauses = new List<string>();

            if (TryAppendTenantFilter(selectable, whereClauses, parameters))
            {
                // tenant filter added
            }

            if (HasColumn(selectable, "Deleted"))
            {
                whereClauses.Add("[Deleted] = 0");
            }

            foreach (var filter in request.Filters ?? Array.Empty<DataFilter>())
            {
                AppendFilterClause(filter, selectable, whereClauses, parameters);
            }

            var whereClause = whereClauses.Count > 0 ? $" WHERE {string.Join(" AND ", whereClauses)}" : string.Empty;
            var orderClause = BuildOrderClause(request.Sorts, selectable, defaultOrder);
            var pagingClause = BuildPagingClause();

            var dataSql = $"SELECT {columnList} FROM {tableIdentifier}{whereClause} {orderClause} {pagingClause}";
            var countSql = $"SELECT COUNT(1) FROM {tableIdentifier}{whereClause}";

            var data = await _db.ExecuteQueryAsync(dataSql, parameters).ConfigureAwait(false);
            var totalObj = await _db.ExecuteScalarAsync(countSql, parameters).ConfigureAwait(false);
            var total = Convert.ToInt32(totalObj ?? 0, CultureInfo.InvariantCulture);

            var rows = ConvertToRows(data);
            return new DataPage(rows, total);
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
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException(nameof(tableName));
            }

            if (string.IsNullOrWhiteSpace(primaryKeyName))
            {
                throw new ArgumentNullException(nameof(primaryKeyName));
            }

            var (_, fields) = await GetMetadataAsync(tableName).ConfigureAwait(false);
            var selectable = fields.Where(f => f.IsLinkToBdd).ToList();
            if (selectable.Count == 0)
            {
                return new Dictionary<string, object?>();
            }

            var tableIdentifier = SqlIdentifierHelper.QuoteTable(tableName);
            var columnList = string.Join(", ", selectable.Select(f => SqlIdentifierHelper.QuoteColumn(f.Libelle)));
            var primaryColumn = SqlIdentifierHelper.QuoteColumn(primaryKeyName);

            var filters = new List<string> { $"{primaryColumn} = @id" };
            var parameters = new Dictionary<string, object?> { ["@id"] = id };

            if (TryAppendTenantFilter(selectable, filters, parameters))
            {
                // Tenant filter appended.
            }

            if (HasColumn(selectable, "Deleted"))
            {
                filters.Add("[Deleted] = 0");
            }

            var whereClause = string.Join(" AND ", filters);
            var sql = $"SELECT {columnList} FROM {tableIdentifier} WHERE {whereClause}";
            var result = await _db.ExecuteQueryAsync(sql, parameters).ConfigureAwait(false);
            if (result.Rows.Count == 0)
            {
                return new Dictionary<string, object?>();
            }

            var row = result.Rows[0];
            return row.Table.Columns.Cast<DataColumn>().ToDictionary(col => col.ColumnName, col => row[col]);
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
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException(nameof(tableName));
            }

            if (values is null || values.Count == 0)
            {
                throw new ArgumentException("Values cannot be null or empty", nameof(values));
            }

            var (tableMeta, fields) = await GetMetadataAsync(tableName).ConfigureAwait(false);
            var fieldLookup = fields.Where(f => f.IsLinkToBdd)
                .ToDictionary(f => f.Libelle, StringComparer.OrdinalIgnoreCase);

            var sanitized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in values)
            {
                if (fieldLookup.ContainsKey(kvp.Key))
                {
                    sanitized[fieldLookup[kvp.Key].Libelle] = kvp.Value;
                }
            }

            if (fieldLookup.ContainsKey("TenantId"))
            {
                sanitized["TenantId"] = _user.TenantId;
            }

            if (sanitized.Count == 0)
            {
                throw new ArgumentException("No matching columns found for insert operation.", nameof(values));
            }

            var columnNames = sanitized.Keys.ToArray();
            var columnList = string.Join(", ", columnNames.Select(SqlIdentifierHelper.QuoteColumn));
            var parameterList = string.Join(", ", columnNames.Select(c => "@" + c));
            var paramDict = sanitized.ToDictionary(kvp => "@" + kvp.Key, kvp => kvp.Value);

            var sql = $"INSERT INTO {SqlIdentifierHelper.QuoteTable(tableName)} ({columnList}) VALUES ({parameterList})";
            var affected = await _db.ExecuteNonQueryAsync(sql, paramDict).ConfigureAwait(false);

            if (tableMeta.IsHistorise)
            {
                var historised = new Dictionary<string, object?>();
                foreach (var field in fields)
                {
                    if (!field.IsHistorise)
                    {
                        continue;
                    }

                    if (sanitized.TryGetValue(field.Libelle, out var val))
                    {
                        historised[field.Libelle] = val;
                    }
                }

                if (historised.Count > 0)
                {
                    await _historizer.SaveHistoryAsync(tableName, "INSERT", 0, historised, null).ConfigureAwait(false);
                }
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
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException(nameof(tableName));
            }

            if (string.IsNullOrWhiteSpace(primaryKeyName))
            {
                throw new ArgumentNullException(nameof(primaryKeyName));
            }

            if (values is null || values.Count == 0)
            {
                throw new ArgumentException("Values cannot be null or empty", nameof(values));
            }

            var (tableMeta, fields) = await GetMetadataAsync(tableName).ConfigureAwait(false);
            var writableFields = fields.Where(f => f.IsLinkToBdd).ToList();
            var lookup = writableFields.ToDictionary(f => f.Libelle, StringComparer.OrdinalIgnoreCase);

            var sanitized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in values)
            {
                if (lookup.TryGetValue(kvp.Key, out var field))
                {
                    sanitized[field.Libelle] = kvp.Value;
                }
            }

            sanitized.Remove("TenantId");

            if (sanitized.Count == 0)
            {
                throw new ArgumentException("No matching columns found for update operation.", nameof(values));
            }

            var setClauses = sanitized.Keys.Select(k => $"{SqlIdentifierHelper.QuoteColumn(k)} = @{k}").ToArray();
            var parameters = sanitized.ToDictionary(kvp => "@" + kvp.Key, kvp => kvp.Value);
            parameters["@id"] = id;

            var whereConditions = new List<string> { $"{SqlIdentifierHelper.QuoteColumn(primaryKeyName)} = @id" };
            if (TryAppendTenantFilter(writableFields, whereConditions, parameters))
            {
                // tenant guard added
            }

            var sql = $"UPDATE {SqlIdentifierHelper.QuoteTable(tableName)} SET {string.Join(", ", setClauses)} WHERE {string.Join(" AND ", whereConditions)}";

            IDictionary<string, object?>? beforeUpdate = null;
            if (tableMeta.IsHistorise)
            {
                beforeUpdate = await GetByIdAsync(tableName, primaryKeyName, id).ConfigureAwait(false);
            }

            var affected = await _db.ExecuteNonQueryAsync(sql, parameters).ConfigureAwait(false);

            if (tableMeta.IsHistorise && beforeUpdate is not null)
            {
                var newValues = new Dictionary<string, object?>();
                var oldValues = new Dictionary<string, object?>();

                foreach (var field in fields)
                {
                    if (!field.IsHistorise)
                    {
                        continue;
                    }

                    if (sanitized.TryGetValue(field.Libelle, out var newVal))
                    {
                        newValues[field.Libelle] = newVal;
                    }

                    if (beforeUpdate.TryGetValue(field.Libelle, out var oldVal))
                    {
                        oldValues[field.Libelle] = oldVal;
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
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException(nameof(tableName));
            }

            if (string.IsNullOrWhiteSpace(primaryKeyName))
            {
                throw new ArgumentNullException(nameof(primaryKeyName));
            }

            var (tableMeta, fields) = await GetMetadataAsync(tableName).ConfigureAwait(false);
            var writableFields = fields.Where(f => f.IsLinkToBdd).ToList();

            var parameters = new Dictionary<string, object?> { ["@id"] = id };
            var conditions = new List<string> { $"{SqlIdentifierHelper.QuoteColumn(primaryKeyName)} = @id" };
            if (TryAppendTenantFilter(writableFields, conditions, parameters))
            {
                // tenant guard added
            }

            var sql = $"DELETE FROM {SqlIdentifierHelper.QuoteTable(tableName)} WHERE {string.Join(" AND ", conditions)}";

            IDictionary<string, object?>? existingRow = null;
            if (tableMeta.IsHistorise)
            {
                existingRow = await GetByIdAsync(tableName, primaryKeyName, id).ConfigureAwait(false);
            }

            var affected = await _db.ExecuteNonQueryAsync(sql, parameters).ConfigureAwait(false);

            if (tableMeta.IsHistorise && existingRow is not null)
            {
                var oldValues = new Dictionary<string, object?>();
                foreach (var field in fields)
                {
                    if (!field.IsHistorise)
                    {
                        continue;
                    }

                    if (existingRow.TryGetValue(field.Libelle, out var value))
                    {
                        oldValues[field.Libelle] = value;
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
