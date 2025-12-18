using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aion.DataEngine.Interfaces;

namespace Aion.DataEngine.Tests
{
    public sealed class InMemoryDataProvider : IDataProvider
    {
        private sealed class Table
        {
            public int NextId = 1;
            public readonly List<Dictionary<string, object?>> Rows = new();
        }

        private readonly Dictionary<string, Table> _tables = new(StringComparer.OrdinalIgnoreCase);

        private Table GetTable(string name)
        {
            if (!_tables.TryGetValue(name, out var t))
            {
                t = new Table();
                _tables[name] = t;
            }
            return t;
        }

        public Task<int> ExecuteNonQueryAsync(string sql, IDictionary<string, object?>? parameters = null)
        {
            parameters ??= new Dictionary<string, object?>();
            sql = sql.Trim();

            // UPDATE dbo.X SET ... WHERE ID=@ID
            var um = Regex.Match(sql, @"UPDATE\s+(?:dbo\.)?([^\s]+)\s+SET\s+(.+?)\s+WHERE\s+ID\s*=\s*@ID", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (um.Success)
            {
                var table = um.Groups[1].Value;
                var setPart = um.Groups[2].Value;
                var t = GetTable(table);
                var id = Convert.ToInt32(parameters["@ID"]);
                var row = t.Rows.FirstOrDefault(r => Convert.ToInt32(r["ID"]) == id);
                if (row == null) return Task.FromResult(0);

                foreach (var assign in setPart.Split(','))
                {
                    var parts = assign.Split('=');
                    var col = parts[0].Trim().Trim('[', ']');
                    var par = parts[1].Trim();
                    if (par.StartsWith("@"))
                        row[col] = parameters[par];
                }
                return Task.FromResult(1);
            }

            // UPDATE F_Document SET Deleted=1 ... WHERE ID=@id (already covered by above)

            return Task.FromResult(0);
        }

        public Task<object?> ExecuteScalarAsync(string sql, IDictionary<string, object?>? parameters = null)
        {
            parameters ??= new Dictionary<string, object?>();
            sql = sql.Trim();

            // INSERT INTO dbo.X(...) VALUES(...); SELECT SCOPE_IDENTITY();
            var im = Regex.Match(sql, @"INSERT\s+INTO\s+(?:dbo\.)?([^\(]+)\((.+?)\)\s+VALUES\((.+?)\);\s*SELECT", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (im.Success)
            {
                var table = im.Groups[1].Value.Trim();
                var cols = im.Groups[2].Value.Split(',').Select(s => s.Trim().Trim('[', ']')).ToArray();
                var pars = im.Groups[3].Value.Split(',').Select(s => s.Trim()).ToArray();

                var t = GetTable(table);
                var id = t.NextId++;
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["ID"] = id };
                for (int i = 0; i < cols.Length; i++)
                {
                    var par = pars[i];
                    row[cols[i]] = par.StartsWith("@") ? parameters[par] : par.Trim('\'');
                }
                if (!row.ContainsKey("Actif")) row["Actif"] = true;
                if (!row.ContainsKey("Doc")) row["Doc"] = false;
                if (!row.ContainsKey("Deleted")) row["Deleted"] = false;
                GetTable(table).Rows.Add(row);
                return Task.FromResult<object?>(id);
            }

            // SELECT COUNT(1) FROM dbo.F_Document WHERE TableName=@t AND RecID=@r AND Deleted=0
            var cm = Regex.Match(sql, @"SELECT\s+COUNT\(1\)\s+FROM\s+(?:dbo\.)?F_Document", RegexOptions.IgnoreCase);
            if (cm.Success)
            {
                var t = GetTable("F_Document");
                var tableName = (string)parameters["@t"];
                var recId = Convert.ToInt32(parameters["@r"]);
                var count = t.Rows.Count(r => string.Equals((string)r["TableName"], tableName, StringComparison.OrdinalIgnoreCase)
                                              && Convert.ToInt32(r["RecID"]) == recId
                                              && Convert.ToBoolean(r["Deleted"]) == false);
                return Task.FromResult<object?>(count);
            }

            return Task.FromResult<object?>(null);
        }

        public Task<DataTable> ExecuteQueryAsync(string sql, IDictionary<string, object?>? parameters = null)
        {
            parameters ??= new Dictionary<string, object?>();
            sql = sql.Trim();

            // SELECT TableName, RecID FROM dbo.F_Document WHERE ID=@id
            var selDoc = Regex.Match(sql, @"SELECT\s+TableName,\s*RecID\s+FROM\s+(?:dbo\.)?F_Document\s+WHERE\s+ID\s*=\s*@id", RegexOptions.IgnoreCase);
            if (selDoc.Success)
            {
                var t = GetTable("F_Document");
                var id = Convert.ToInt32(parameters["@id"]);
                var row = t.Rows.FirstOrDefault(r => Convert.ToInt32(r["ID"]) == id);
                var dt = new DataTable();
                dt.Columns.Add("TableName", typeof(string));
                dt.Columns.Add("RecID", typeof(int));
                if (row != null)
                    dt.Rows.Add(row["TableName"], Convert.ToInt32(row["RecID"]));
                return Task.FromResult(dt);
            }

            // SELECT * FROM dbo.F_Document WHERE TableName=@t AND RecID=@r [AND Deleted=0]
            var selDocs = Regex.Match(sql, @"SELECT\s+\*\s+FROM\s+(?:dbo\.)?F_Document\s+WHERE\s+TableName", RegexOptions.IgnoreCase);
            if (selDocs.Success)
            {
                var t = GetTable("F_Document");
                var tableName = (string)parameters["@t"];
                var recId = Convert.ToInt32(parameters["@r"]);
                var withDeleted = !sql.Contains("AND Deleted=0");
                var rows = t.Rows.Where(r => string.Equals((string)r["TableName"], tableName, StringComparison.OrdinalIgnoreCase)
                                             && Convert.ToInt32(r["RecID"]) == recId
                                             && (withDeleted || Convert.ToBoolean(r["Deleted"]) == false)).ToList();
                var dt = new DataTable();
                // Build columns from first row or default set
                var cols = new[] { "ID","TableName","RecID","Categorie","Path","Extension","Actif","Doc","Deleted","DtCreation","DtModification","DtSuppression","UsrCreationId","UsrModificationId","UsrSuppressionId" };
                foreach (var c in cols) dt.Columns.Add(c, typeof(object));
                foreach (var r in rows)
                {
                    var dr = dt.NewRow();
                    foreach (var c in cols) dr[c] = r.ContainsKey(c) ? r[c] ?? DBNull.Value : DBNull.Value;
                    dt.Rows.Add(dr);
                }
                return Task.FromResult(dt);
            }

            return Task.FromResult(new DataTable());
        }
    }
}
