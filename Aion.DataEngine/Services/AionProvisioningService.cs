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
        private readonly IUserContext _userContext;

        public AionProvisioningService(IDataProvider db, IUserContext userContext, IClock clock)
        {
            _db = db;
            _clock = clock;
            _userContext = userContext;
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

            // Catalogue des tables existantes dans STable et SField
            DataEngine dataEngine = new(_db, _userContext, _clock);
            await dataEngine.SynchronizeSystemCatalogAsync();

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
    ParentId INT NULL,
    ModuleId INT NULL,
    Libelle NVARCHAR(255) NOT NULL,
    IsLeaf BIT NOT NULL DEFAULT(1),
    Icon NVARCHAR(100) NULL,
    [Order] INT NOT NULL DEFAULT(0),
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
    CONSTRAINT UQ_SMenu_Code UNIQUE(Libelle)
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
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(500) NULL,
    Route NVARCHAR(500) NOT NULL,
    Icon NVARCHAR(100) NULL,
    Color NVARCHAR(20) NULL,
    [Order] INT NOT NULL DEFAULT(0),
    TenantId INT NOT NULL DEFAULT(1),
    Actif BIT NOT NULL DEFAULT(1),
    Doc BIT NOT NULL DEFAULT(0),
    Deleted BIT NOT NULL DEFAULT(0),
    DtCreation DATETIME NOT NULL DEFAULT(GETUTCDATE()),
    CONSTRAINT UQ_Module_Code UNIQUE(Name)
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
    Libelle NVARCHAR(255) NOT NULL,
    Description NVARCHAR(500) NULL,
    Parent NVARCHAR(255) NULL,
    ParentLiaison NVARCHAR(255) NULL,
    ReferentielLibelle NVARCHAR(255) NULL,
    Type NVARCHAR(5) NULL,
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
    CONSTRAINT UQ_STable_Nom UNIQUE(Libelle)
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
    Libelle NVARCHAR(255) NOT NULL,
    Alias NVARCHAR(255) NOT NULL,
    DataType NVARCHAR(15) NOT NULL,
    Taille INT NOT NULL DEFAULT(1),
    IsClePrimaire BIT DEFAULT(0), 
    IsUnique BIT DEFAULT(0), 
    Referentiel NVARCHAR(255) NULL,
    ReferentielWhereClause NVARCHAR(255) NULL,
    Defaut NVARCHAR(255) NULL,
    IsNulleable BIT NOT NULL DEFAULT(1),
    [Precision] INT NULL,
    Echelle INT NULL,
    Nullable BIT NOT NULL DEFAULT(1),
    IsHistorise BIT NOT NULL DEFAULT(0),
    Regex NVARCHAR(MAX)  NULL,
    CoordonneeLabelX INT NOT NULL DEFAULT(0),
    CoordonneeLabelY INT NOT NULL DEFAULT(0),
    CoordonneeX INT NOT NULL DEFAULT(0),
    CoordonneeY INT NOT NULL DEFAULT(0),
    Format NVARCHAR(255) NULL,
    IsLinkToBdd BIT DEFAULT(1),
    IsVisible BIT DEFAULT(1),
    Masque NVARCHAR(255) NULL,
    Max NVARCHAR(255) NULL,
    Min NVARCHAR(255) NULL,
    Ordre INT NOT NULL DEFAULT(0),
    IsSearch BIT DEFAULT(0),
    SearchDefautValue NVARCHAR(255) NULL,
    SearchOperator NVARCHAR(15) NULL,
    ValidationScript NVARCHAR(MAX) NULL,
    ValidationYaml NVARCHAR(MAX) NULL,
    Commentaire NVARCHAR(MAX) NULL,
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
  
  CREATE UNIQUE INDEX UX_SField_Table_Col ON dbo.SField(TableId, Libelle);
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

    -- =====================================================================
    -- Insertion des Regex utiles pour un ERP
    -- =====================================================================
    INSERT INTO RRegex (Code, Pattern, Description)
    VALUES
    (
        'EMAIL_STD',
        '^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$',
        'Adresse email standard. Valide la structure générale nom@domaine.extension.'
    ),
    (
        'TEL_FR_SIMPLE',
        '^(?:(?:\+|00)33|0)\s*[1-9](?:[\s.-]*\d{2}){4}$',
        'Numéro de téléphone français (fixe ou mobile). Accepte les formats 0X, +33 X, 0033 X avec espaces, points ou tirets.'
    ),
    (
        'CP_FR',
        '^[0-9]{5}$',
        'Code Postal français. Exactement 5 chiffres.'
    ),
    (
        'CP_FR_DEP',
        '^(?:0[1-9]|[1-8]\d|9[0-57-8])\d{3}$',
        'Code Postal français (plus strict). Valide les numéros de départements métropolitains (01-95) et DOM (97, 98).'
    ),
    (
        'TVA_UE_GENERIC',
        '^((AT)U\d{8}|(BE)0\d{9}|(BG)\d{9,10}|(CY)\d{8}[A-Z]|(CZ)\d{8,10}|(DE)\d{9}|(DK)\d{8}|(EE)\d{9}|(EL|GR)\d{9}|(ES)[A-Z0-9]\d{7}[A-Z0-9]|(FI)\d{8}|(FR)[A-Z0-9]{2}\d{9}|(HR)\d{11}|(HU)\d{8}|(IE)[A-Z0-9]{7}[A-Z]|[A-Z0-9]{7}[A-Z]{2}|(IT)\d{11}|(LT)\d{9,12}|(LU)\d{8}|(LV)\d{11}|(MT)\d{8}|(NL)\d{9}B\d{2}|(PL)\d{10}|(PT)\d{9}|(RO)\d{2,10}|(SE)\d{12}|(SI)\d{8}|(SK)\d{10})$',
        'Numéro de TVA intracommunautaire européen. Valide le format par pays (préfixe + structure).'
    ),
    (
        'IBAN_GENERIC',
        '^[A-Z]{2}[0-9]{2}[A-Z0-9]{1,30}$',
        'IBAN (International Bank Account Number). Valide la structure générique (2 lettres pays, 2 chiffres clé, 1-30 alphanumériques).'
    ),
    (
        'SKU_SIMPLE',
        '^[A-Z0-9-]{3,20}$',
        'Code article (SKU) simple. Accepte 3 à 20 caractères alphanumériques majuscules et tirets.'
    ),
    (
        'SKU_FORMATTE',
        '^[A-Z]{3}-\d{5}$',
        'Code article (SKU) formaté. Exemple : 3 lettres, un tiret, 5 chiffres (ex: ABC-12345).'
    ),
    (
        'NUM_COMMANDE',
        '^CMD-[0-9]{4}-[0-9]{5}$',
        'Numéro de commande. Exemple : préfixe CMD-, 4 chiffres (année), 5 chiffres (séquence).'
    ),
    (
        'DATE_ISO',
        '^\d{4}-\d{2}-\d{2}$',
        'Date au format ISO 8601 (YYYY-MM-DD).'
    ),
    (
        'HEURE_24H',
        '^([01]\d|2[0-3]):([0-5]\d)$',
        'Heure au format 24h (HH:MM). Valide de 00:00 à 23:59.'
    ),
    (
        'CODE_ANALYTIQUE',
        '^[A-Z]{2,5}-\d{3,6}$',
        'Code analytique / Centre de coût. Exemple : 2-5 lettres, tiret, 3-6 chiffres.'
    ),
    (
        'PASSWORD_STRONG',
        '^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$',
        'Mot de passe fort. Requis d''au moins 8 caractères, 1 minuscule, 1 majuscule, 1 chiffre et 1 caractère spécial.'
    ),
    (
        'NUMERIQUE_SEUL',
        '^\d+$',
        'Numérique uniquement. Accepte un ou plusieurs chiffres, sans signe.'
    ),
    (
        'DECIMAL_VIRGULE',
        '^\d+([,]\d{1,2})?$',
        'Nombre décimal (max 2 décimales) avec virgule comme séparateur. (Ex: 123 ou 123,45)'
    ),
    (
        'DECIMAL_POINT',
        '^\d+([.]\d{1,2})?$',
        'Nombre décimal (max 2 décimales) avec point comme séparateur. (Ex: 123 ou 123.45)'
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
  END TRY
  BEGIN CATCH
    PRINT ERROR_MESSAGE();
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