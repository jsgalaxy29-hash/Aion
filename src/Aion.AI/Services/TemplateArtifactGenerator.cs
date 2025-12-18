using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Abstractions;
using Aion.AI.Models;

namespace Aion.AI.Services;

/// <summary>
/// Generates opinionated artifacts for demonstration purposes.
/// </summary>
public sealed class TemplateArtifactGenerator : IArtifactGenerator
{
    private readonly ILanguageModelClient _languageModelClient;

    public TemplateArtifactGenerator(ILanguageModelClient languageModelClient)
    {
        _languageModelClient = languageModelClient;
    }

    public async Task<ArtifactGenerationResult> GenerateAsync(GenerationPlan plan, string patchYaml, SimulationResult simulation, CancellationToken ct = default)
    {
        var result = new ArtifactGenerationResult();
        var artifacts = new List<GeneratedArtifact>();

        if (plan.ModuleName.Equals("Contrats", StringComparison.OrdinalIgnoreCase))
        {
            artifacts.Add(new GeneratedArtifact
            {
                ArtifactType = "Entity",
                RelativePath = "Aion.Domain/Contrats/Contrat.cs",
                Content = GetContratEntity()
            });

            artifacts.Add(new GeneratedArtifact
            {
                ArtifactType = "Entity",
                RelativePath = "Aion.Domain/Contrats/Assure.cs",
                Content = GetAssureEntity()
            });

            artifacts.Add(new GeneratedArtifact
            {
                ArtifactType = "Configuration",
                RelativePath = "Aion.Infrastructure/Configurations/Contrats/ContratConfiguration.cs",
                Content = GetContratConfiguration()
            });

            artifacts.Add(new GeneratedArtifact
            {
                ArtifactType = "Configuration",
                RelativePath = "Aion.Infrastructure/Configurations/Contrats/AssureConfiguration.cs",
                Content = GetAssureConfiguration()
            });

            artifacts.Add(new GeneratedArtifact
            {
                ArtifactType = "Migration",
                RelativePath = "Aion.Infrastructure/Migrations/20240101090000_CreateContratsModule.cs",
                Content = GetMigrationSnippet()
            });

            artifacts.Add(new GeneratedArtifact
            {
                ArtifactType = "Endpoint",
                RelativePath = "Aion.AppHost/Endpoints/ContratsEndpoints.cs",
                Content = GetEndpointSnippet()
            });

            artifacts.Add(new GeneratedArtifact
            {
                ArtifactType = "Blazor",
                RelativePath = "Aion.AppHost/Pages/Contrats/ContratList.razor",
                Content = GetContratListComponent()
            });

            artifacts.Add(new GeneratedArtifact
            {
                ArtifactType = "Blazor",
                RelativePath = "Aion.AppHost/Pages/Contrats/ContratForm.razor",
                Content = GetContratFormComponent()
            });
        }
        else
        {
            var response = await _languageModelClient.CompleteAsync("Generate summary", plan.ModuleName, ct).ConfigureAwait(false);
            result.Warnings.Add($"Contenu généré par LLM: {response}");
        }

        result.Artifacts = artifacts;
        return result;
    }

    private static string GetContratEntity() => """
using System;
using System.Collections.Generic;
using Aion.Domain.Common;

namespace Aion.Domain.Contrats;

public class Contrat : BaseEntity
{
    public string Numero { get; set; } = string.Empty;

    public DateTimeOffset DateEffet { get; set; }

    public DateTimeOffset? DateEcheance { get; set; }

    public decimal PrimeAnnuelle { get; set; }

    public string Statut { get; set; } = "Brouillon";

    public ICollection<Assure> Assures { get; set; } = new List<Assure>();
}
""";

    private static string GetAssureEntity() => """
using System;
using Aion.Domain.Common;

namespace Aion.Domain.Contrats;

public class Assure : BaseEntity
{
    public string Nom { get; set; } = string.Empty;

    public string Prenom { get; set; } = string.Empty;

    public DateTimeOffset? DateNaissance { get; set; }

    public string Email { get; set; } = string.Empty;

    public Guid ContratId { get; set; }

    public Contrat? Contrat { get; set; }
}
""";

    private static string GetContratConfiguration() => """
using Aion.Domain.Contrats;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aion.Infrastructure.Configurations.Contrats;

public class ContratConfiguration : IEntityTypeConfiguration<Contrat>
{
    public void Configure(EntityTypeBuilder<Contrat> builder)
    {
        builder.ToTable("FContrat");
        builder.Property(x => x.Numero).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Statut).IsRequired().HasMaxLength(50);
        builder.Property(x => x.PrimeAnnuelle).HasPrecision(18, 2);
        builder.HasIndex(x => x.Numero).IsUnique();
        builder.HasMany(x => x.Assures)
            .WithOne(x => x.Contrat)
            .HasForeignKey(x => x.ContratId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
""";

    private static string GetAssureConfiguration() => """
using Aion.Domain.Contrats;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aion.Infrastructure.Configurations.Contrats;

public class AssureConfiguration : IEntityTypeConfiguration<Assure>
{
    public void Configure(EntityTypeBuilder<Assure> builder)
    {
        builder.ToTable("FAssure");
        builder.Property(x => x.Nom).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Prenom).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(200);
        builder.HasIndex(x => x.Email).IsUnique();
    }
}
""";

    private static string GetMigrationSnippet() => """
// Migration CreateContratsModule
// Creates FContrat and FAssure tables with the proper constraints.
""";

    private static string GetEndpointSnippet() => """
// Minimal API endpoints for Contrat CRUD and action ValiderContrat
""";

    private static string GetContratListComponent() => """
@page "/contrats"
<h1>Contrats</h1>
<!-- Table rendering contrats -->
""";

    private static string GetContratFormComponent() => """
@page "/contrats/nouveau"
<h1>Créer un contrat</h1>
<!-- Formulaire simple -->
""";
}
