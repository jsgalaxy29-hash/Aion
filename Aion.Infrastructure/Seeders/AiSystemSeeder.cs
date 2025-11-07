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
        await SeedConfigAsync(dbContext, cancellationToken).ConfigureAwait(false);
        await SeedSynonymsAsync(dbContext, cancellationToken).ConfigureAwait(false);
        await SeedTemplatesAsync(dbContext, cancellationToken).ConfigureAwait(false);
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
