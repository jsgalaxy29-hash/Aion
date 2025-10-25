using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Aion.DataEngine.Interfaces;

namespace Aion.DataEngine.Services
{
    public sealed class AionProvisioningService : IAionProvisioningService
    {
        private readonly IDataProvider _db;
        private readonly IClock _clock;

        public AionProvisioningService(IDataProvider db, IClock clock)
        {
            _db = db; _clock = clock;
        }

        public async Task EnsureDatabaseReadyAsync()
        {
            // 1) Core: security + catalogs + regex + metadata + documents + base-entity columns
            await _db.ExecuteNonQueryAsync(SqlSecurityCreate());
            await _db.ExecuteNonQueryAsync(SqlCreateCatalogs());
            await _db.ExecuteNonQueryAsync(SqlCreateRegex());
            await _db.ExecuteNonQueryAsync(SqlCreateMetaTables());
            await _db.ExecuteNonQueryAsync(SqlCreateFDocument());
            await _db.ExecuteNonQueryAsync(SqlAddBaseEntityColumnsMacro());

            // 2) Seed Admin group/user + right types
            var adminGroupId = await EnsureAdminGroupAsync();
            var adminUserId = await EnsureAdminUserAsync();
            await EnsureRightTypesAsync();

            // 3) Populate S_TABLE/S_CHAMP from INFORMATION_SCHEMA if empty or missing rows
            await PopulateMetadataAsync();

            // 4) Grant default rights (Menu/Table/Module/Action/Report) to Admin and to all members of Administrateur
            await GrantAdminDefaultsAsync(adminUserId);
            await GrantGroupDefaultsAsync("Administrateur");

            // 5) Detect new F_* tables and ensure Admin rights
            await GrantAdminForNewFTablesAsync(adminUserId);
        }

        #region SQL builders

        private static string SqlSecurityCreate() => @"
IF OBJECT_ID('dbo.SGroupe','U') IS NULL
BEGIN
  CREATE TABLE dbo.SGroupe(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Nom NVARCHAR(128) NOT NULL UNIQUE,
    Actif BIT NOT NULL DEFAULT(1),
    Deleted BIT NOT NULL DEFAULT(0),
    DtCreation DATETIME NOT NULL DEFAULT(GETUTCDATE())
  );
END;

IF OBJECT_ID('dbo.SUser','U') IS NULL
BEGIN
  CREATE TABLE dbo.SUser(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Login NVARCHAR(128) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    Actif BIT NOT NULL DEFAULT(1),
    Deleted BIT NOT NULL DEFAULT(0),
    DtCreation DATETIME NOT NULL DEFAULT(GETUTCDATE())
  );
END;

IF OBJECT_ID('dbo.SUserGroupe','U') IS NULL
BEGIN
  CREATE TABLE dbo.SUserGroupe(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    GroupeId INT NOT NULL,
    CONSTRAINT FK_SUG_User FOREIGN KEY(UserId) REFERENCES dbo.SUser(ID),
    CONSTRAINT FK_SUG_Groupe FOREIGN KEY(GroupeId) REFERENCES dbo.SGroupe(ID),
    CONSTRAINT UQ_SUG UNIQUE(UserId, GroupeId)
  );
END;

IF OBJECT_ID('dbo.SRightType','U') IS NULL
BEGIN
  CREATE TABLE dbo.SRightType(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(32) NOT NULL UNIQUE,
    Libelle NVARCHAR(128) NOT NULL
  );
END;

IF OBJECT_ID('dbo.SRight','U') IS NULL
BEGIN
  CREATE TABLE dbo.SRight(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    SubjectType NVARCHAR(32) NOT NULL,  -- MENU, TABLE, MODULE, ACTION, REPORT
    SubjectId INT NOT NULL,
    Droit1 BIT NOT NULL DEFAULT(0),
    Droit2 BIT NULL,
    Droit3 BIT NULL,
    CONSTRAINT UQ_SRight UNIQUE(UserId, SubjectType, SubjectId),
    CONSTRAINT FK_SR_User FOREIGN KEY(UserId) REFERENCES dbo.SUser(ID)
  );
END;
";

        private static string SqlCreateCatalogs() => @"
IF OBJECT_ID('dbo.SMenu','U') IS NULL
  CREATE TABLE dbo.SMenu(ID INT IDENTITY(1,1) PRIMARY KEY, Code NVARCHAR(128) NOT NULL UNIQUE);
IF OBJECT_ID('dbo.SModule','U') IS NULL
  CREATE TABLE dbo.SModule(ID INT IDENTITY(1,1) PRIMARY KEY, Code NVARCHAR(128) NOT NULL UNIQUE);
IF OBJECT_ID('dbo.SAction','U') IS NULL
  CREATE TABLE dbo.SAction(ID INT IDENTITY(1,1) PRIMARY KEY, Code NVARCHAR(128) NOT NULL UNIQUE);
IF OBJECT_ID('dbo.SReport','U') IS NULL
  CREATE TABLE dbo.SReport(ID INT IDENTITY(1,1) PRIMARY KEY, Code NVARCHAR(128) NOT NULL UNIQUE);
";

        private static string SqlCreateRegex() => @"
IF OBJECT_ID('dbo.R_REGEX','U') IS NULL
BEGIN
  CREATE TABLE dbo.R_REGEX(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(100) NOT NULL UNIQUE,
    Pattern NVARCHAR(4000) NOT NULL,
    Description NVARCHAR(400) NULL
  );
END;
";

        private static string SqlCreateMetaTables() => @"
IF OBJECT_ID('dbo.S_TABLE','U') IS NULL
BEGIN
  CREATE TABLE dbo.S_TABLE(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Nom NVARCHAR(255) NOT NULL UNIQUE,   -- table name
    Libelle NVARCHAR(255) NULL,
    IsHistorise BIT NOT NULL DEFAULT(0),
    Actif BIT NOT NULL DEFAULT(1),
    Doc BIT NOT NULL DEFAULT(0),
    Deleted BIT NOT NULL DEFAULT(0),
    DtCreation DATETIME NOT NULL DEFAULT(GETUTCDATE()),
    DtModification DATETIME NULL,
    DtSuppression DATETIME NULL,
    UsrCreationId INT NULL,
    UsrModificationId INT NULL,
    UsrSuppressionId INT NULL,
    RowVersion ROWVERSION
  );
END;

IF OBJECT_ID('dbo.S_CHAMP','U') IS NULL
BEGIN
  CREATE TABLE dbo.S_CHAMP(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    TableId INT NOT NULL,
    Nom NVARCHAR(255) NOT NULL,          -- column name
    TypeSql NVARCHAR(100) NOT NULL,
    Longueur INT NULL,
    Precision INT NULL,
    Echelle INT NULL,
    Nullable BIT NOT NULL DEFAULT(1),
    IsHistorise BIT NOT NULL DEFAULT(0),
    RegexId INT NULL,
    Actif BIT NOT NULL DEFAULT(1),
    Doc BIT NOT NULL DEFAULT(0),
    Deleted BIT NOT NULL DEFAULT(0),
    DtCreation DATETIME NOT NULL DEFAULT(GETUTCDATE()),
    DtModification DATETIME NULL,
    DtSuppression DATETIME NULL,
    UsrCreationId INT NULL,
    UsrModificationId INT NULL,
    UsrSuppressionId INT NULL,
    RowVersion ROWVERSION,
    CONSTRAINT FK_SCH_Table FOREIGN KEY(TableId) REFERENCES dbo.S_TABLE(ID)
  );
  CREATE UNIQUE INDEX UX_SCH_Table_Col ON dbo.S_CHAMP(TableId, Nom);
END;
";

        private static string SqlCreateFDocument() => @"
IF OBJECT_ID('dbo.F_Document','U') IS NULL
BEGIN
  CREATE TABLE dbo.F_Document(
      ID INT IDENTITY(1,1) PRIMARY KEY,
      TableName VARCHAR(255) NOT NULL,
      RecID INT NOT NULL,
      Categorie VARCHAR(100) NULL,
      [Path] VARCHAR(2000) NOT NULL,
      Extension VARCHAR(20) NULL,
      Actif BIT NOT NULL DEFAULT(1),
      Doc BIT NOT NULL DEFAULT(0),
      Deleted BIT NOT NULL DEFAULT(0),
      DtCreation DATETIME NOT NULL DEFAULT (GETUTCDATE()),
      DtModification DATETIME NULL,
      DtSuppression DATETIME NULL,
      UsrCreationId INT NULL,
      UsrModificationId INT NULL,
      UsrSuppressionId INT NULL,
      RowVersion ROWVERSION
  );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_FDocument_Table_Rec' AND object_id=OBJECT_ID('dbo.F_Document'))
  CREATE INDEX IX_FDocument_Table_Rec ON dbo.F_Document(TableName, RecID) INCLUDE (Deleted);
";

        private static string SqlAddBaseEntityColumnsMacro() => @"
DECLARE @targets TABLE(Name sysname);
INSERT INTO @targets(Name)
VALUES ('dbo.S_TABLE'),
       ('dbo.S_CHAMP'),
       ('dbo.R_REGEX'),
       ('dbo.S_SOURCE_EXTERNE'),
       ('dbo.S_SOURCE_BINDING'),
       ('dbo.S_HISTO_VERSION'),
       ('dbo.S_HISTO_CHANGE');

DECLARE @t sysname, @sql nvarchar(max);
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT Name FROM @targets;
OPEN cur; FETCH NEXT FROM cur INTO @t;
WHILE @@FETCH_STATUS = 0
BEGIN
  SET @sql = N'
    IF COL_LENGTH('''+@t+''',''Actif'') IS NULL    ALTER TABLE '+@t+' ADD Actif BIT NOT NULL DEFAULT(1);
    IF COL_LENGTH('''+@t+''',''Doc'') IS NULL      ALTER TABLE '+@t+' ADD Doc BIT NOT NULL DEFAULT(0);
    IF COL_LENGTH('''+@t+''',''Deleted'') IS NULL  ALTER TABLE '+@t+' ADD Deleted BIT NOT NULL DEFAULT(0);
    IF COL_LENGTH('''+@t+''',''DtCreation'') IS NULL     ALTER TABLE '+@t+' ADD DtCreation DATETIME NOT NULL DEFAULT (GETUTCDATE());
    IF COL_LENGTH('''+@t+''',''DtModification'') IS NULL ALTER TABLE '+@t+' ADD DtModification DATETIME NULL;
    IF COL_LENGTH('''+@t+''',''DtSuppression'') IS NULL  ALTER TABLE '+@t+' ADD DtSuppression DATETIME NULL;
    IF COL_LENGTH('''+@t+''',''UsrCreationId'') IS NULL  ALTER TABLE '+@t+' ADD UsrCreationId INT NULL;
    IF COL_LENGTH('''+@t+''',''UsrModificationId'') IS NULL ALTER TABLE '+@t+' ADD UsrModificationId INT NULL;
    IF COL_LENGTH('''+@t+''',''UsrSuppressionId'') IS NULL  ALTER TABLE '+@t+' ADD UsrSuppressionId INT NULL;
    IF COL_LENGTH('''+@t+''',''RowVersion'') IS NULL ALTER TABLE '+@t+' ADD RowVersion ROWVERSION;
  ';
  EXEC sp_executesql @sql;
  FETCH NEXT FROM cur INTO @t;
END
CLOSE cur; DEALLOCATE cur;
";

        #endregion

        #region Seeding helpers

        private async Task<int> EnsureAdminGroupAsync()
        {
            var idObj = await _db.ExecuteScalarAsync("SELECT ID FROM dbo.SGroupe WHERE Nom=@n", new Dictionary<string, object?> { ["@n"]="Administrateur" });
            if (idObj is int id) return id;     
            var newId = await _db.ExecuteScalarAsync(
                "INSERT INTO dbo.SGroupe(Nom) VALUES(@n); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new Dictionary<string, object?> { ["@n"]="Administrateur" });
            return Convert.ToInt32(newId);
        }

        private async Task<int> EnsureAdminUserAsync()
        {
            var idObj = await _db.ExecuteScalarAsync("SELECT ID FROM dbo.SUser WHERE Login=@l", new Dictionary<string, object?> { ["@l"]="Admin" });
            int uid;
            if (idObj is int id) uid = id;
            else
            {
                // Default password 'Admin' hashed with SHA-256 (replace later by stronger hash).
                var hash = SimpleSha256("Admin");
                var newId = await _db.ExecuteScalarAsync(
                    "INSERT INTO dbo.SUser(Login, PasswordHash) VALUES(@l,@p); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new Dictionary<string, object?> { ["@l"]="Admin", ["@p"]=hash });
                uid = Convert.ToInt32(newId);
            }

            var gid = await _db.ExecuteScalarAsync("SELECT ID FROM dbo.SGroupe WHERE Nom=@n", new Dictionary<string, object?> { ["@n"]="Administrateur" });
            if (gid is int groupId)
            {
                await _db.ExecuteNonQueryAsync(
                    "IF NOT EXISTS(SELECT 1 FROM dbo.SUserGroupe WHERE UserId=@u AND GroupeId=@g) INSERT INTO dbo.SUserGroupe(UserId,GroupeId) VALUES(@u,@g);",
                    new Dictionary<string, object?> { ["@u"]=uid, ["@g"]=groupId });
            }
            return uid;
        }

        private static string SimpleSha256(string s)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(s);
            return Convert.ToHexString(sha.ComputeHash(bytes));
        }

        private async Task EnsureRightTypesAsync()
        {
            async Task Seed(string code, string lib)
            {
                var exists = await _db.ExecuteScalarAsync("SELECT 1 FROM dbo.SRightType WHERE Code=@c", new Dictionary<string, object?> { ["@c"]=code });
                if (exists == null)
                    await _db.ExecuteNonQueryAsync("INSERT INTO dbo.SRightType(Code, Libelle) VALUES(@c,@l)", new Dictionary<string, object?> { ["@c"]=code, ["@l"]=lib });
            }
            await Seed("MENU",   "Accès menu (Autorisé)");
            await Seed("TABLE",  "Accès table (Lecture/Ecriture/Suppression)");
            await Seed("MODULE", "Accès module (Autorisé)");
            await Seed("ACTION", "Accès action (Autorisé)");
            await Seed("REPORT", "Accès rapport (Autorisé)");
        }

        #endregion

        #region Metadata population

        private async Task PopulateMetadataAsync()
        {
            // Insert S_TABLE rows for all user tables not yet present
            var tables = await _db.ExecuteQueryAsync(@"
                SELECT s.name AS SchemaName, t.name AS TableName
                FROM sys.tables t
                JOIN sys.schemas s ON t.schema_id = s.schema_id
                WHERE s.name = 'dbo';", null);

            foreach (DataRow tr in tables.Rows)
            {
                var tableName = (string)tr["TableName"];
                // Ensure S_TABLE entry
                var exists = await _db.ExecuteScalarAsync("SELECT ID FROM dbo.S_TABLE WHERE Nom=@n", new Dictionary<string, object?> { ["@n"]=tableName });
                int tableId;
                if (exists is int id) tableId = id;
                else
                {
                    var newId = await _db.ExecuteScalarAsync(
                        "INSERT INTO dbo.S_TABLE(Nom, Libelle, IsHistorise) VALUES(@n,@l,0); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                        new Dictionary<string, object?> { ["@n"]=tableName, ["@l"]=tableName });
                    tableId = Convert.ToInt32(newId);
                }

                // Ensure S_CHAMP entries for each column
                var cols = await _db.ExecuteQueryAsync(@"
                    SELECT c.name AS ColName, ty.name AS TypeName, c.max_length, c.precision, c.scale, c.is_nullable
                    FROM sys.columns c
                    JOIN sys.types ty ON c.user_type_id = ty.user_type_id
                    WHERE c.object_id = OBJECT_ID(@full);",
                    new Dictionary<string, object?> { ["@full"] = "dbo."+tableName });

                foreach (DataRow cr in cols.Rows)
                {
                    var col = (string)cr["ColName"];
                    var present = await _db.ExecuteScalarAsync(
                        "SELECT ID FROM dbo.S_CHAMP WHERE TableId=@tid AND Nom=@c",
                        new Dictionary<string, object?> { ["@tid"]=tableId, ["@c"]=col });
                    if (present != null) continue;

                    await _db.ExecuteNonQueryAsync(
                        @"INSERT INTO dbo.S_CHAMP(TableId, Nom, TypeSql, Longueur, Precision, Echelle, Nullable, IsHistorise)
                          VALUES(@tid,@n,@t,@len,@prec,@scale,@nulls, 0);",
                        new Dictionary<string, object?> {
                            ["@tid"]=tableId,
                            ["@n"]=col,
                            ["@t"]= (string)cr["TypeName"],
                            ["@len"]= Convert.ToInt32(cr["max_length"]),
                            ["@prec"]= Convert.ToInt32(cr["precision"]),
                            ["@scale"]= Convert.ToInt32(cr["scale"]),
                            ["@nulls"]= Convert.ToBoolean(cr["is_nullable"])
                        });
                }
            }
        }

        #endregion

        #region Rights assignment

        private async Task GrantAdminDefaultsAsync(int adminUserId)
        {
            await GrantAllOfAsync("SMenu",   "MENU",   adminUserId, d1:true);
            await GrantAllOfAsync("S_TABLE", "TABLE",  adminUserId, d1:true, d2:true, d3:true);
            await GrantAllOfAsync("SModule", "MODULE", adminUserId, d1:true);
            await GrantAllOfAsync("SAction", "ACTION", adminUserId, d1:true);
            await GrantAllOfAsync("SReport", "REPORT", adminUserId, d1:true);
        }

        private async Task GrantGroupDefaultsAsync(string groupName)
        {
            var gidObj = await _db.ExecuteScalarAsync("SELECT ID FROM dbo.SGroupe WHERE Nom=@n", new Dictionary<string, object?> { ["@n"]=groupName });
            if (gidObj is not int gid) return;
            var users = await _db.ExecuteQueryAsync("SELECT u.ID FROM dbo.SUser u JOIN dbo.SUserGroupe ug ON ug.UserId=u.ID AND ug.GroupeId=@g", new Dictionary<string, object?> { ["@g"]=gid });
            foreach (DataRow r in users.Rows)
            {
                int uid = Convert.ToInt32(r["ID"]);
                await GrantAdminDefaultsAsync(uid);
            }
        }

        private async Task GrantAdminForNewFTablesAsync(int adminUserId)
        {
            // Any dbo.F_* table: ensure S_TABLE exists + rights
            var ftables = await _db.ExecuteQueryAsync(@"
                SELECT t.name AS TableName
                FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
                WHERE s.name='dbo' AND t.name LIKE 'F[_]%';", null);

            foreach (DataRow tr in ftables.Rows)
            {
                var tableName = (string)tr["TableName"];
                var stexists = await _db.ExecuteScalarAsync("SELECT ID FROM dbo.S_TABLE WHERE Nom=@n", new Dictionary<string, object?> { ["@n"]=tableName });
                if (stexists == null)
                {
                    var tid = await _db.ExecuteScalarAsync(
                        "INSERT INTO dbo.S_TABLE(Nom, Libelle, IsHistorise) VALUES(@n,@l,0); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                        new Dictionary<string, object?> { ["@n"]=tableName, ["@l"]=tableName });
                    // also populate S_CHAMP for this table
                    // (PopulateMetadataAsync will fill missing; we can call it again or rely on the next run.)
                }

                // Rights for Admin on this table
                await GrantAllOfAsync("S_TABLE", "TABLE", adminUserId, d1:true, d2:true, d3:true);
            }
        }

        private async Task GrantAllOfAsync(string table, string subjectType, int userId, bool? d1=null, bool? d2=null, bool? d3=null)
        {
            // If table doesn't exist, skip
            var exists = await _db.ExecuteScalarAsync(
                "SELECT 1 FROM sys.objects WHERE object_id=OBJECT_ID(@t) AND type='U'",
                new Dictionary<string, object?> { ["@t"]="dbo."+table });
            if (exists == null) return;

            var dt = await _db.ExecuteQueryAsync($"SELECT ID FROM dbo.{table}", null);
            foreach (DataRow r in dt.Rows)
            {
                var subjectId = Convert.ToInt32(r["ID"]);
                var already = await _db.ExecuteScalarAsync(
                    "SELECT 1 FROM dbo.SRight WHERE UserId=@u AND SubjectType=@st AND SubjectId=@sid",
                    new Dictionary<string, object?> { ["@u"]=userId, ["@st"]=subjectType, ["@sid"]=subjectId });
                if (already != null) continue;

                await _db.ExecuteNonQueryAsync(
                    "INSERT INTO dbo.SRight(UserId, SubjectType, SubjectId, Droit1, Droit2, Droit3) VALUES(@u,@st,@sid,@d1,@d2,@d3)",
                    new Dictionary<string, object?> {
                        ["@u"]=userId, ["@st"]=subjectType, ["@sid"]=subjectId,
                        ["@d1"]= (object?) d1 ?? false,
                        ["@d2"]= (object?) d2 ?? DBNull.Value,
                        ["@d3"]= (object?) d3 ?? DBNull.Value
                    });
            }
        }

        #endregion
    }
}
