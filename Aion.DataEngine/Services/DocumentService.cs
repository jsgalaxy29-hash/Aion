using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Aion.DataEngine.Entities;
using Aion.DataEngine.Interfaces;

namespace Aion.DataEngine.Services
{
    public sealed class DocumentService : IDocumentService
    {
        private readonly IDataProvider _db;
        private readonly IUserContext _user;
        private readonly IClock _clock;

        public DocumentService(IDataProvider db, IUserContext user, IClock clock)
        { _db = db; _user = user; _clock = clock; }

        public async Task<int> LinkAsync(string tableName, int recId, string path, string? categorie = null, string? extension = null)
        {
            var now = _clock.UtcNow;
            var uid = _user.UserId;

            var id = await _db.ExecuteScalarAsync(@"
                INSERT INTO dbo.FDocument(TableName, RecID, Categorie, [Path], Extension, TenantId,
                                           Actif, Doc, Deleted, DtCreation, UsrCreationId)
                VALUES(@t,@r,@c,@p,@e,@tenant, 1, 0, 0, @now, @uid);
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new Dictionary<string, object?> { ["@t"]=tableName, ["@r"]=recId, ["@c"]=categorie, ["@p"]=path, ["@e"]=extension, ["@tenant"]=_user.TenantId, ["@now"]=now, ["@uid"]=uid });

            var tableIdentifier = SqlIdentifierHelper.QuoteTable(tableName);
            await _db.ExecuteNonQueryAsync($@"UPDATE {tableIdentifier} SET Doc = 1, DtModification=@now, UsrModificationId=@uid WHERE Id=@id AND TenantId=@tenant",
                new Dictionary<string, object?> { ["@id"]=recId, ["@now"]=now, ["@uid"]=uid, ["@tenant"]=_user.TenantId });

            return Convert.ToInt32(id);
        }

        public async Task UnlinkAsync(int docId)
        {
            var now = _clock.UtcNow;
            var uid = _user.UserId;

            var dt = await _db.ExecuteQueryAsync("SELECT TableName, RecID FROM dbo.FDocument WHERE ID=@id",
                new Dictionary<string, object?> { ["@id"]=docId });
            if (dt.Rows.Count == 0) return;

            var table = (string)dt.Rows[0]["TableName"];
            var recId = Convert.ToInt32(dt.Rows[0]["RecID"]);

            await _db.ExecuteNonQueryAsync(
                "UPDATE dbo.FDocument SET Deleted=1, DtSuppression=@now, UsrSuppressionId=@uid WHERE ID=@id",
                new Dictionary<string, object?> { ["@id"]=docId, ["@now"]=now, ["@uid"]=uid });

            var remain = await _db.ExecuteScalarAsync(
                "SELECT COUNT(1) FROM dbo.FDocument WHERE TableName=@t AND RecID=@r AND Deleted=0",
                new Dictionary<string, object?> { ["@t"]=table, ["@r"]=recId });

            if (Convert.ToInt32(remain) == 0)
            {
                var tableIdentifier = SqlIdentifierHelper.QuoteTable(table);
                await _db.ExecuteNonQueryAsync(
                    $@"UPDATE {tableIdentifier} SET Doc = 0, DtModification=@now, UsrModificationId=@uid WHERE Id=@id AND TenantId=@tenant",
                    new Dictionary<string, object?> { ["@id"]=recId, ["@now"]=now, ["@uid"]=uid, ["@tenant"]=_user.TenantId });
            }
        }

        public async Task<IEnumerable<FDocument>> GetAsync(string tableName, int recId, bool withDeleted = false)
        {
            var sql = "SELECT * FROM dbo.FDocument WHERE TableName=@t AND RecID=@r AND TenantId=@tenant";
            if (!withDeleted) sql += " AND Deleted=0";

            var dt = await _db.ExecuteQueryAsync(sql, new Dictionary<string, object?> { ["@t"]=tableName, ["@r"]=recId, ["@tenant"]=_user.TenantId });
            return dt.AsEnumerable().Select(r => new FDocument
            {
                Id = Convert.ToInt32(r["ID"]),
                TableName = (string)r["TableName"],
                RecordId = Convert.ToInt32(r["RecID"]),
                Categorie = r["Categorie"] as string,
                Path = (string)r["Path"],
                Extension = r["Extension"] as string,
                Actif = Convert.ToBoolean(r["Actif"]),
                Doc = Convert.ToBoolean(r["Doc"]),
                Deleted = Convert.ToBoolean(r["Deleted"]),
                DtCreation = Convert.ToDateTime(r["DtCreation"]),
                DtModification = r["DtModification"] as DateTime?,
                DtSuppression = r["DtSuppression"] as DateTime?,
                UsrCreationId = r["UsrCreationId"] as int?,
                UsrModificationId = r["UsrModificationId"] as int?,
                UsrSuppressionId = r["UsrSuppressionId"] as int?
            });
        }

    }
}
