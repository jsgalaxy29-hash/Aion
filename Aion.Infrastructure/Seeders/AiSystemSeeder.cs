using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.AI;
using Microsoft.EntityFrameworkCore;

namespace Aion.Infrastructure.Seeders;

public static class AiSystemSeeder
{
    public static async Task SeedAsync(AionDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await EnsureAiTablesExistAsync(dbContext, cancellationToken).ConfigureAwait(false);
        await SeedConfigAsync(dbContext, cancellationToken).ConfigureAwait(false);
        await SeedSynonymsAsync(dbContext, cancellationToken).ConfigureAwait(false);
        await SeedTemplatesAsync(dbContext, cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureAiTablesExistAsync(AionDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsSqlServer())
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            const string ensureConfigSql = @"
IF OBJECT_ID(N'dbo.SXAiConfig', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SXAiConfig
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SXAiConfig PRIMARY KEY,
        TenantId INT NOT NULL DEFAULT(1),
        Actif BIT NOT NULL DEFAULT(1),
        Doc BIT NOT NULL DEFAULT(0),
        Deleted BIT NOT NULL DEFAULT(0),
        DtCreation DATETIME2 NOT NULL DEFAULT(GETUTCDATE()),
        DtModification DATETIME2 NULL,
        DtSuppression DATETIME2 NULL,
        UsrCreationId INT NULL,
        UsrModificationId INT NULL,
        UsrSuppressionId INT NULL,
        RowVersion ROWVERSION NULL,
        Provider NVARCHAR(50) NOT NULL,
        ApiKey NVARCHAR(200) NULL,
        BaseUrl NVARCHAR(200) NULL,
        ModelName NVARCHAR(100) NOT NULL,
        Temperature FLOAT NOT NULL,
        MaxTokens INT NOT NULL,
        IsEnabled BIT NOT NULL DEFAULT(1)
    );
END";

            const string ensureSynonymSql = @"
IF OBJECT_ID(N'dbo.SXSynonym', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SXSynonym
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SXSynonym PRIMARY KEY,
        TenantId INT NOT NULL DEFAULT(1),
        Actif BIT NOT NULL DEFAULT(1),
        Doc BIT NOT NULL DEFAULT(0),
        Deleted BIT NOT NULL DEFAULT(0),
        DtCreation DATETIME2 NOT NULL DEFAULT(GETUTCDATE()),
        DtModification DATETIME2 NULL,
        DtSuppression DATETIME2 NULL,
        UsrCreationId INT NULL,
        UsrModificationId INT NULL,
        UsrSuppressionId INT NULL,
        RowVersion ROWVERSION NULL,
        DomainTerm NVARCHAR(100) NOT NULL,
        SynonymsCsv NVARCHAR(500) NOT NULL,
        Category NVARCHAR(50) NOT NULL
    );

    CREATE UNIQUE INDEX IX_SXSynonym_DomainTerm ON dbo.SXSynonym(DomainTerm);
END";

            const string ensureTemplateSql = @"
IF OBJECT_ID(N'dbo.SXTemplate', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SXTemplate
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SXTemplate PRIMARY KEY,
        TenantId INT NOT NULL DEFAULT(1),
        Actif BIT NOT NULL DEFAULT(1),
        Doc BIT NOT NULL DEFAULT(0),
        Deleted BIT NOT NULL DEFAULT(0),
        DtCreation DATETIME2 NOT NULL DEFAULT(GETUTCDATE()),
        DtModification DATETIME2 NULL,
        DtSuppression DATETIME2 NULL,
        UsrCreationId INT NULL,
        UsrModificationId INT NULL,
        UsrSuppressionId INT NULL,
        RowVersion ROWVERSION NULL,
        TemplateKey NVARCHAR(100) NOT NULL,
        TemplateType NVARCHAR(50) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        IsActive BIT NOT NULL DEFAULT(1)
    );

    CREATE UNIQUE INDEX IX_SXTemplate_TemplateKey ON dbo.SXTemplate(TemplateKey);
END";

            const string ensureGenerationLogSql = @"
IF OBJECT_ID(N'dbo.SXGenerationLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SXGenerationLog
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SXGenerationLog PRIMARY KEY,
        TenantId INT NOT NULL DEFAULT(1),
        Actif BIT NOT NULL DEFAULT(1),
        Doc BIT NOT NULL DEFAULT(0),
        Deleted BIT NOT NULL DEFAULT(0),
        DtCreation DATETIME2 NOT NULL DEFAULT(GETUTCDATE()),
        DtModification DATETIME2 NULL,
        DtSuppression DATETIME2 NULL,
        UsrCreationId INT NULL,
        UsrModificationId INT NULL,
        UsrSuppressionId INT NULL,
        RowVersion ROWVERSION NULL,
        RequestText NVARCHAR(MAX) NOT NULL,
        IntentsJson NVARCHAR(MAX) NULL,
        PlanJson NVARCHAR(MAX) NULL,
        PatchYaml NVARCHAR(MAX) NULL,
        ArtifactsSummary NVARCHAR(MAX) NULL,
        Status NVARCHAR(32) NOT NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        ModelVersion NVARCHAR(64) NOT NULL
    );
END";

            await dbContext.Database.ExecuteSqlRawAsync(ensureConfigSql, cancellationToken).ConfigureAwait(false);
            await dbContext.Database.ExecuteSqlRawAsync(ensureSynonymSql, cancellationToken).ConfigureAwait(false);
            await dbContext.Database.ExecuteSqlRawAsync(ensureTemplateSql, cancellationToken).ConfigureAwait(false);
            await dbContext.Database.ExecuteSqlRawAsync(ensureGenerationLogSql, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task SeedConfigAsync(AionDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!await dbContext.SXAiConfigs.IgnoreQueryFilters().AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            dbContext.SXAiConfigs.Add(new SXAiConfig
            {
                Provider = "Mock",
                ModelName = "mock-gpt",
                Temperature = 0.1d,
                MaxTokens = 2048,
                IsEnabled = true,
                BaseUrl = "http://localhost"
            });

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task SeedSynonymsAsync(AionDbContext dbContext, CancellationToken cancellationToken)
    {
        var defaults = new List<SXSynonym>
        {
            new()
            {
                DomainTerm = "Gestionnaire",
                SynonymsCsv = "manager,administrateur",
                Category = "Role"
            },
            new()
            {
                DomainTerm = "Contrat",
                SynonymsCsv = "contract,agreement",
                Category = "Entity"
            },
            new()
            {
                DomainTerm = "Valider",
                SynonymsCsv = "approuver,confirmer",
                Category = "Action"
            }
        };

        foreach (var synonym in defaults)
        {
            var exists = await dbContext.SXSynonyms.IgnoreQueryFilters()
                .AnyAsync(x => x.DomainTerm == synonym.DomainTerm, cancellationToken)
                .ConfigureAwait(false);

            if (!exists)
            {
                dbContext.SXSynonyms.Add(synonym);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedTemplatesAsync(AionDbContext dbContext, CancellationToken cancellationToken)
    {
        var promptTemplate = await dbContext.SXTemplates.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TemplateKey == "DefaultSystemPrompt", cancellationToken)
            .ConfigureAwait(false);

        if (promptTemplate is null)
        {
            dbContext.SXTemplates.Add(new SXTemplate
            {
                TemplateKey = "DefaultSystemPrompt",
                TemplateType = "Prompt",
                Content = "Tu es l'orchestrateur IA d'Aion. Respecte les conventions de nommage et génère des plans détaillés.",
                IsActive = true
            });
        }

        var artifactTemplate = await dbContext.SXTemplates.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TemplateKey == "ContratCrudSnippet", cancellationToken)
            .ConfigureAwait(false);

        if (artifactTemplate is null)
        {
            dbContext.SXTemplates.Add(new SXTemplate
            {
                TemplateKey = "ContratCrudSnippet",
                TemplateType = "Endpoint",
                Content = "Endpoint de validation pour les contrats, accessible aux rôles Gestionnaire.",
                IsActive = true
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
