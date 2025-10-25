using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aion.DataEngine.Interfaces;

namespace Aion.DataEngine.Services
{
    public partial class DataEngine
    {
        private readonly IDataProvider _db;
        private readonly IUserContext _user;
        private readonly IClock _clock;

        public DataEngine(IDataProvider db, IUserContext user, IClock clock)
        {
            _db = db; _user = user; _clock = clock;
        }

        public async Task<int> InsertAsync(string tableName, IDictionary<string, object?> values)
        {
            if (!values.ContainsKey("Actif")) values["Actif"] = true;
            if (!values.ContainsKey("Doc")) values["Doc"] = false;
            if (!values.ContainsKey("Deleted")) values["Deleted"] = false;
            values["DtCreation"] = _clock.UtcNow;
            values["UsrCreationId"] = _user.UserId;

            var cols = string.Join(", ", values.Keys.Select(k => $"[{k}]"));
            var pars = string.Join(", ", values.Keys.Select(k => $"@{k}"));
            var sql = $"INSERT INTO dbo.{tableName} ({cols}) VALUES ({pars}); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var id = await _db.ExecuteScalarAsync(sql, ToParams(values));
            return Convert.ToInt32(id);
        }

        public async Task<int> UpdateAsync(string tableName, int id, IDictionary<string, object?> values, byte[]? rowVersion = null)
        {
            values["DtModification"] = _clock.UtcNow;
            values["UsrModificationId"] = _user.UserId;

            var sets = string.Join(", ", values.Keys.Select(k => $"[{k}]=@{k}"));
            var where = "WHERE ID=@ID";
            var pars = ToParams(values);
            pars["@ID"] = id;

            if (rowVersion is not null)
            {
                where += " AND RowVersion=@RowVersion";
                pars["@RowVersion"] = rowVersion;
            }

            var sql = $"UPDATE dbo.{tableName} SET {sets} {where};";
            var rows = await _db.ExecuteNonQueryAsync(sql, pars);
            if (rowVersion is not null && rows == 0)
                throw new InvalidOperationException("Conflit de concurrence (RowVersion).");

            return rows;
        }

        public async Task<int> DeleteAsync(string tableName, int id, byte[]? rowVersion = null)
        {
            var now = _clock.UtcNow;
            var uid = _user.UserId;

            var pars = new Dictionary<string, object?> { ["@ID"]=id, ["@now"]=now, ["@uid"]=uid };
            var where = "WHERE ID=@ID";
            if (rowVersion is not null) { where += " AND RowVersion=@RowVersion"; pars["@RowVersion"] = rowVersion; }

            var sql = $@"UPDATE dbo.{tableName}
                         SET Deleted=1, DtSuppression=@now, UsrSuppressionId=@uid
                         {where};";
            var rows = await _db.ExecuteNonQueryAsync(sql, pars);
            if (rowVersion is not null && rows == 0)
                throw new InvalidOperationException("Conflit de concurrence (RowVersion).");

            return rows;
        }

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
    }
}
