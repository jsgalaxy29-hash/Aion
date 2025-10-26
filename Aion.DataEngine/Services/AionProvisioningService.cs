using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Aion.DataEngine.Interfaces;

namespace Aion.DataEngine.Services
{
    /// <summary>
    /// Service de provisioning Aion - Crée la structure SQL de base.
    /// Version corrigée et compatible avec le nouveau schéma de sécurité.
    /// </summary>
    public sealed class AionProvisioningService : IAionProvisioningService
    {
        private readonly IDataProvider _db;
        private readonly IClock _clock;

        public AionProvisioningService(IDataProvider db, IClock clock)
        {
            _db = db;
            _clock = clock;
        }

        public async Task EnsureDatabaseReadyAsync()
        {
            Console.WriteLine("🔧 AionProvisioningService : Création de la structure SQL...");

            // 1) Tables de sécurité
            await _db.ExecuteNonQueryAsync(SqlSecurityCreate());
            Console.WriteLine("   ✅ Tables de sécurité créées");

            // 2) Tables catalogues (Menu, Module, Action, Report)
            await _db.ExecuteNonQueryAsync(SqlCreateCatalogs());
            Console.WriteLine("   ✅ Tables catalogues créées");

            // 3) Tables métadonnées (STable, SField)
            await _db.ExecuteNonQueryAsync(SqlCreateMetaTables());
            Console.WriteLine("   ✅ Tables métadonnées créées");

            // 4) Table Regex
            await _db.ExecuteNonQueryAsync(SqlCreateRegex());
            Console.WriteLine("   ✅ Table Regex créée");

            // 5) Table Documents
            await _db.ExecuteNonQueryAsync(SqlCreateFDocument());
            Console.WriteLine("   ✅ Table Documents créée");

            // 6) Ajout colonnes BaseEntity aux tables système
            await _db.ExecuteNonQueryAsync(SqlAddBaseEntityColumnsMacro());
            Console.WriteLine("   ✅ Colonnes BaseEntity ajoutées");

            Console.WriteLine("✅ Structure SQL complète créée");
        }

        #region SQL Builders

        private static string SqlSecurityCreate() => @"
-- ===== TABLES DE SÉCURITÉ (nouveau schéma) =====

-- Table SGroup (groupes d'utilisateurs)
IF OBJECT_ID('dbo.SGroup','U') IS NULL
BEGIN
  CREATE TABLE dbo.SGroup(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(128) NOT NULL,
    Description NVARCHAR(500) NULL,
    IsSystem BIT NOT NULL DEFAULT(0),
    TenantId INT NOT NULL DEFAULT(1),
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
    CONSTRAINT UQ_SGroup_Name_Tenant UNIQUE(Name, TenantId)
  );
  PRINT 'Table SGroup créée';
END
ELSE
  PRINT 'Table SGroup existe déjà';

-- Table SUser (utilisateurs)
IF OBJECT_ID('dbo.SUser','U') IS NULL
BEGIN
  CREATE TABLE dbo.SUser(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    UserName NVARCHAR(128) NOT NULL,
    NormalizedUserName NVARCHAR(128) NULL,
    Email NVARCHAR(256) NULL,
    NormalizedEmail NVARCHAR(256) NULL,
    PasswordHash NVARCHAR(512) NOT NULL,
    FullName NVARCHAR(256) NULL,
    IsActive BIT NOT NULL DEFAULT(1),
    LastLoginDate DATETIME NULL,
    AccessFailedCount INT NOT NULL DEFAULT(0),
    LockoutEnd DATETIME NULL,
    TenantId INT NOT NULL DEFAULT(1),
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
    CONSTRAINT UQ_SUser_NormalizedUserName UNIQUE(NormalizedUserName)
  );
  CREATE INDEX IX_SUser_Email ON dbo.SUser(NormalizedEmail);
  CREATE INDEX IX_SUser_Tenant ON dbo.SUser(TenantId, Deleted);
  PRINT 'Table SUser créée';
END
ELSE
  PRINT 'Table SUser existe déjà';

-- Table SUserGroup (association user-groupe)
IF OBJECT_ID('dbo.SUserGroup','U') IS NULL
BEGIN
  CREATE TABLE dbo.SUserGroup(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    GroupId INT NOT NULL,
    IsLinkActive BIT NOT NULL DEFAULT(1),
    TenantId INT NOT NULL DEFAULT(1),
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
  
  -- Contraintes FK (ajoutées après création des tables)
  IF OBJECT_ID('dbo.SUser', 'U') IS NOT NULL
    ALTER TABLE dbo.SUserGroup ADD CONSTRAINT FK_SUG_User FOREIGN KEY(UserId) REFERENCES dbo.SUser(ID) ON DELETE CASCADE;
  IF OBJECT_ID('dbo.SGroup', 'U') IS NOT NULL
    ALTER TABLE dbo.SUserGroup ADD CONSTRAINT FK_SUG_Group FOREIGN KEY(GroupId) REFERENCES dbo.SGroup(ID) ON DELETE CASCADE;
  
  CREATE UNIQUE INDEX UQ_SUserGroup ON dbo.SUserGroup(UserId, GroupId, TenantId);
  PRINT 'Table SUserGroup créée';
END
ELSE
  PRINT 'Table SUserGroup existe déjà';

-- Table SRightType (types de droits)
IF OBJECT_ID('dbo.SRightType','U') IS NULL
BEGIN
  CREATE TABLE dbo.SRightType(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(32) NOT NULL,
    Name NVARCHAR(128) NOT NULL,
    DataSource NVARCHAR(255) NOT NULL DEFAULT(''),
    Right1Name NVARCHAR(128) NOT NULL DEFAULT(''),
    Right2Name NVARCHAR(128) NOT NULL DEFAULT(''),
    Right3Name NVARCHAR(128) NOT NULL DEFAULT(''),
    Right4Name NVARCHAR(128) NOT NULL DEFAULT(''),
    Right5Name NVARCHAR(128) NOT NULL DEFAULT(''),
    [Order] INT NOT NULL DEFAULT(0),
    IsActive BIT NOT NULL DEFAULT(1),
    TenantId INT NOT NULL DEFAULT(1),
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
    CONSTRAINT UQ_SRightType_Code_Tenant UNIQUE(Code, TenantId)
  );
  PRINT 'Table SRightType créée';
END
ELSE
  PRINT 'Table SRightType existe déjà';

-- Table SRight (droits par groupe)
IF OBJECT_ID('dbo.SRight','U') IS NULL
BEGIN
  CREATE TABLE dbo.SRight(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    GroupId INT NOT NULL,
    Target NVARCHAR(32) NOT NULL,        -- Menu, Module, Table, Action, Report
    SubjectId INT NOT NULL,              -- ID de la ressource
    Right1 BIT NULL,
    Right2 BIT NULL,
    Right3 BIT NULL,
    Right4 BIT NULL,
    Right5 BIT NULL,
    TenantId INT NOT NULL DEFAULT(1),
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
  
  -- Contrainte FK
  IF OBJECT_ID('dbo.SGroup', 'U') IS NOT NULL
    ALTER TABLE dbo.SRight ADD CONSTRAINT FK_SR_Group FOREIGN KEY(GroupId) REFERENCES dbo.SGroup(ID) ON DELETE CASCADE;
  
  CREATE UNIQUE INDEX UQ_SRight ON dbo.SRight(GroupId, Target, SubjectId, TenantId);
  CREATE INDEX IX_SRight_Target ON dbo.SRight(Target, SubjectId);
  PRINT 'Table SRight créée';
END
ELSE
  PRINT 'Table SRight existe déjà';
";

        private static string SqlCreateCatalogs() => @"
-- ===== TABLES CATALOGUES =====

IF OBJECT_ID('dbo.SMenu','U') IS NULL
BEGIN
  CREATE TABLE dbo.SMenu(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(128) NOT NULL,
    Libelle NVARCHAR(255) NOT NULL,
    Route NVARCHAR(500) NULL,
    ParentId INT NULL,
    IsLeaf BIT NOT NULL DEFAULT(0),
    Icon NVARCHAR(100) NULL,
    [Order] INT NOT NULL DEFAULT(0),
    IsActive BIT NOT NULL DEFAULT(1),
    TenantId INT NOT NULL DEFAULT(1),
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
    CONSTRAINT UQ_SMenu_Code UNIQUE(Code)
  );
  
  -- FK auto-référence
  ALTER TABLE dbo.SMenu ADD CONSTRAINT FK_SMenu_Parent FOREIGN KEY(ParentId) REFERENCES dbo.SMenu(ID);
  PRINT 'Table SMenu créée';
END
ELSE
  PRINT 'Table SMenu existe déjà';

IF OBJECT_ID('dbo.SModule','U') IS NULL
BEGIN
  CREATE TABLE dbo.SModule(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(128) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(500) NULL,
    Icon NVARCHAR(100) NULL,
    Color NVARCHAR(20) NULL,
    [Order] INT NOT NULL DEFAULT(0),
    IsActive BIT NOT NULL DEFAULT(1),
    TenantId INT NOT NULL DEFAULT(1),
    Actif BIT NOT NULL DEFAULT(1),
    Doc BIT NOT NULL DEFAULT(0),
    Deleted BIT NOT NULL DEFAULT(0),
    DtCreation DATETIME NOT NULL DEFAULT(GETUTCDATE()),
    CONSTRAINT UQ_Module_Code UNIQUE(Code)
  );
  PRINT 'Table SModule créée';
END
ELSE
  PRINT 'Table SModule existe déjà';

IF OBJECT_ID('dbo.SAction','U') IS NULL
BEGIN
  CREATE TABLE dbo.SAction(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(128) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(500) NULL,
    Type NVARCHAR(50) NULL,
    TenantId INT NOT NULL DEFAULT(1),
    Actif BIT NOT NULL DEFAULT(1),
    Doc BIT NOT NULL DEFAULT(0),
    Deleted BIT NOT NULL DEFAULT(0),
    DtCreation DATETIME NOT NULL DEFAULT(GETUTCDATE()),
    CONSTRAINT UQ_Action_Code UNIQUE(Code)
  );
  PRINT 'Table SAction créée';
END
ELSE
  PRINT 'Table SAction existe déjà';

IF OBJECT_ID('dbo.SReport','U') IS NULL
BEGIN
  CREATE TABLE dbo.SReport(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(128) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(500) NULL,
    Category NVARCHAR(100) NULL,
    TenantId INT NOT NULL DEFAULT(1),
    Actif BIT NOT NULL DEFAULT(1),
    Doc BIT NOT NULL DEFAULT(0),
    Deleted BIT NOT NULL DEFAULT(0),
    DtCreation DATETIME NOT NULL DEFAULT(GETUTCDATE()),
    CONSTRAINT UQ_Report_Code UNIQUE(Code)
  );
  PRINT 'Table SReport créée';
END
ELSE
  PRINT 'Table SReport existe déjà';
";

        private static string SqlCreateMetaTables() => @"
-- ===== TABLES MÉTADONNÉES =====

IF OBJECT_ID('dbo.STable','U') IS NULL
BEGIN
  CREATE TABLE dbo.STable(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Nom NVARCHAR(255) NOT NULL,
    Libelle NVARCHAR(255) NULL,
    IsHistorise BIT NOT NULL DEFAULT(0),
    TenantId INT NOT NULL DEFAULT(1),
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
    CONSTRAINT UQ_STable_Nom UNIQUE(Nom)
  );
  PRINT 'Table STable créée';
END
ELSE
  PRINT 'Table STable existe déjà';

IF OBJECT_ID('dbo.SField','U') IS NULL
BEGIN
  CREATE TABLE dbo.SField(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    TableId INT NOT NULL,
    Nom NVARCHAR(255) NOT NULL,
    TypeSql NVARCHAR(100) NOT NULL,
    Longueur INT NULL,
    [Precision] INT NULL,
    Echelle INT NULL,
    Nullable BIT NOT NULL DEFAULT(1),
    IsHistorise BIT NOT NULL DEFAULT(0),
    RegexId INT NULL,
    TenantId INT NOT NULL DEFAULT(1),
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
  
  -- FK vers STable
  IF OBJECT_ID('dbo.STable', 'U') IS NOT NULL
    ALTER TABLE dbo.SField ADD CONSTRAINT FK_SField_Table FOREIGN KEY(TableId) REFERENCES dbo.STable(ID);
  
  CREATE UNIQUE INDEX UX_SField_Table_Col ON dbo.SField(TableId, Nom);
  PRINT 'Table SField créée';
END
ELSE
  PRINT 'Table SField existe déjà';
";

        private static string SqlCreateRegex() => @"
-- ===== TABLE REGEX =====

IF OBJECT_ID('dbo.RRegex','U') IS NULL
BEGIN
  CREATE TABLE dbo.RRegex(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(100) NOT NULL,
    Pattern NVARCHAR(4000) NOT NULL,
    Description NVARCHAR(400) NULL,
    TenantId INT NOT NULL DEFAULT(1),
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
    CONSTRAINT UQ_Regex_Code UNIQUE(Code)
  );
  PRINT 'Table RRegex créée';
END
ELSE
  PRINT 'Table RRegex existe déjà';
";

        private static string SqlCreateFDocument() => @"
-- ===== TABLE DOCUMENTS =====

IF OBJECT_ID('dbo.FDocument','U') IS NULL
BEGIN
  CREATE TABLE dbo.FDocument(
    ID INT IDENTITY(1,1) PRIMARY KEY,
    TableName NVARCHAR(255) NOT NULL,
    RecID INT NOT NULL,
    Categorie NVARCHAR(100) NULL,
    [Path] NVARCHAR(2000) NOT NULL,
    Extension NVARCHAR(20) NULL,
    TenantId INT NOT NULL DEFAULT(1),
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
  
  CREATE INDEX IX_FDocument_Table_Rec ON dbo.FDocument(TableName, RecID) INCLUDE (Deleted);
  PRINT 'Table FDocument créée';
END
ELSE
  PRINT 'Table FDocument existe déjà';
";

        private static string SqlAddBaseEntityColumnsMacro() => @"
-- ===== AJOUT COLONNES BaseEntity AUX TABLES SYSTÈME =====

DECLARE @targets TABLE(Name NVARCHAR(255));
INSERT INTO @targets(Name)
VALUES 
  ('dbo.STable'),
  ('dbo.SField'),
  ('dbo.RRegex'),
  ('dbo.SMenu'),
  ('dbo.SModule'),
  ('dbo.SAction'),
  ('dbo.SReport');

DECLARE @t NVARCHAR(255), @sql NVARCHAR(MAX);
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR 
  SELECT Name FROM @targets 
  WHERE EXISTS(SELECT 1 FROM sys.objects WHERE object_id=OBJECT_ID(Name) AND type='U');

OPEN cur; 
FETCH NEXT FROM cur INTO @t;

WHILE @@FETCH_STATUS = 0
BEGIN
  SET @sql = N'
    IF COL_LENGTH(''' + @t + ''',''TenantId'') IS NULL 
      ALTER TABLE ' + @t + ' ADD TenantId INT NOT NULL DEFAULT(1);
    IF COL_LENGTH(''' + @t + ''',''Actif'') IS NULL 
      ALTER TABLE ' + @t + ' ADD Actif BIT NOT NULL DEFAULT(1);
    IF COL_LENGTH(''' + @t + ''',''Doc'') IS NULL 
      ALTER TABLE ' + @t + ' ADD Doc BIT NOT NULL DEFAULT(0);
    IF COL_LENGTH(''' + @t + ''',''Deleted'') IS NULL 
      ALTER TABLE ' + @t + ' ADD Deleted BIT NOT NULL DEFAULT(0);
    IF COL_LENGTH(''' + @t + ''',''DtCreation'') IS NULL 
      ALTER TABLE ' + @t + ' ADD DtCreation DATETIME NOT NULL DEFAULT(GETUTCDATE());
    IF COL_LENGTH(''' + @t + ''',''DtModification'') IS NULL 
      ALTER TABLE ' + @t + ' ADD DtModification DATETIME NULL;
    IF COL_LENGTH(''' + @t + ''',''DtSuppression'') IS NULL 
      ALTER TABLE ' + @t + ' ADD DtSuppression DATETIME NULL;
    IF COL_LENGTH(''' + @t + ''',''UsrCreationId'') IS NULL 
      ALTER TABLE ' + @t + ' ADD UsrCreationId INT NULL;
    IF COL_LENGTH(''' + @t + ''',''UsrModificationId'') IS NULL 
      ALTER TABLE ' + @t + ' ADD UsrModificationId INT NULL;
    IF COL_LENGTH(''' + @t + ''',''UsrSuppressionId'') IS NULL 
      ALTER TABLE ' + @t + ' ADD UsrSuppressionId INT NULL;
    IF COL_LENGTH(''' + @t + ''',''RowVersion'') IS NULL 
      ALTER TABLE ' + @t + ' ADD RowVersion ROWVERSION;
  ';
  
  BEGIN TRY
    EXEC sp_executesql @sql;
    PRINT ''Colonnes BaseEntity ajoutées à '' + @t;
  END TRY
  BEGIN CATCH
    PRINT ''Erreur sur '' + @t + '': '' + ERROR_MESSAGE();
  END CATCH
  
  FETCH NEXT FROM cur INTO @t;
END

CLOSE cur; 
DEALLOCATE cur;

PRINT 'Ajout colonnes BaseEntity terminé';
";

        #endregion
    }
}