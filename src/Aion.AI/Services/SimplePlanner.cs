using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Abstractions;
using Aion.AI.Models;

namespace Aion.AI.Services;

/// <summary>
/// Lightweight planner converting intents into a deterministic plan.
/// </summary>
public sealed class SimplePlanner : IPlanner
{
    public Task<GenerationPlan> BuildPlanAsync(IntentRecognitionResult intentResult, CancellationToken ct = default)
    {
        var moduleName = intentResult.Metadata.TryGetValue("module", out var name) && !string.IsNullOrWhiteSpace(name)
            ? Capitalize(name)
            : "Generic";

        var plan = new GenerationPlan
        {
            ModuleName = moduleName
        };

        var steps = new List<GenerationPlanStep>();
        var artifacts = new List<PlannedArtifact>();

        if (intentResult.Intents.Any(i => i.Type == RecognizedIntentType.CreateModule))
        {
            steps.Add(new GenerationPlanStep
            {
                StepType = "RoadmapPatch",
                Description = $"Créer le module {moduleName} dans la roadmap",
            });

            artifacts.Add(new PlannedArtifact
            {
                ArtifactType = "YamlPatch",
                Description = "Patch AION_ROADMAP.yaml",
                TargetPath = "AION_ROADMAP.yaml"
            });
        }

        if (intentResult.Intents.Any(i => i.Type == RecognizedIntentType.GenerateCrud))
        {
            steps.Add(new GenerationPlanStep
            {
                StepType = "Entity",
                Description = "Générer les entités EF Core",
                DependsOn = new List<string> { "RoadmapPatch" }
            });

            steps.Add(new GenerationPlanStep
            {
                StepType = "Migration",
                Description = "Créer la migration EF Core",
                DependsOn = new List<string> { "Entity" }
            });

            steps.Add(new GenerationPlanStep
            {
                StepType = "Api",
                Description = "Générer les endpoints CRUD",
                DependsOn = new List<string> { "Migration" }
            });

            steps.Add(new GenerationPlanStep
            {
                StepType = "Ui",
                Description = "Générer les composants Blazor",
                DependsOn = new List<string> { "Api" }
            });

            artifacts.Add(new PlannedArtifact
            {
                ArtifactType = "CSharp",
                Description = "Entités EF Core",
                TargetPath = $"Aion.Domain/{moduleName}"
            });

            artifacts.Add(new PlannedArtifact
            {
                ArtifactType = "Migration",
                Description = "Migration EF Core",
                TargetPath = "Aion.Infrastructure/Migrations"
            });

            artifacts.Add(new PlannedArtifact
            {
                ArtifactType = "Endpoint",
                Description = "Endpoints Minimal API",
                TargetPath = "Aion.AppHost/Endpoints"
            });

            artifacts.Add(new PlannedArtifact
            {
                ArtifactType = "Blazor",
                Description = "Pages Blazor CRUD",
                TargetPath = $"Aion.AppHost/Pages/{moduleName}"
            });
        }

        plan.Steps = steps;
        plan.Artifacts = artifacts;

        return Task.FromResult(plan);
    }

    private static string Capitalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Generic";
        }

        return char.ToUpperInvariant(value[0]) + value[1..];
    }
}
