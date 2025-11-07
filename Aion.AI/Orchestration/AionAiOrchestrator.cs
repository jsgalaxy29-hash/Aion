using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Abstractions;
using Aion.AI.Models;
using Microsoft.Extensions.Logging;

namespace Aion.AI.Orchestration;

/// <summary>
/// Default orchestrator wiring together all the AI building blocks.
/// </summary>
public sealed class AionAiOrchestrator : IAionAiOrchestrator
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IIntentRecognizer _intentRecognizer;
    private readonly IPlanner _planner;
    private readonly IRoadmapPatcher _roadmapPatcher;
    private readonly ISimulator _simulator;
    private readonly IArtifactGenerator _artifactGenerator;
    private readonly IAuditTrailService _auditTrail;
    private readonly ILogger<AionAiOrchestrator> _logger;

    public AionAiOrchestrator(
        IIntentRecognizer intentRecognizer,
        IPlanner planner,
        IRoadmapPatcher roadmapPatcher,
        ISimulator simulator,
        IArtifactGenerator artifactGenerator,
        IAuditTrailService auditTrail,
        ILogger<AionAiOrchestrator> logger)
    {
        _intentRecognizer = intentRecognizer;
        _planner = planner;
        _roadmapPatcher = roadmapPatcher;
        _simulator = simulator;
        _artifactGenerator = artifactGenerator;
        _auditTrail = auditTrail;
        _logger = logger;
    }

    public async Task<GenerationResult> HandleNaturalLanguageRequestAsync(string requestText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(requestText))
        {
            throw new ArgumentException("Request text cannot be empty", nameof(requestText));
        }

        _logger.LogInformation("🤖 Processing AI generation request: {Request}", requestText);

        // record est visible dans try + catch
        var record = new AuditRecord
        {
            RequestText = requestText,
            Status = GenerationStatus.Draft,
            ModelVersion = "mock-gpt"
        };

        try
        {
            var intents = await _intentRecognizer.RecognizeAsync(requestText, ct).ConfigureAwait(false);
            record.IntentsJson = JsonSerializer.Serialize(intents, SerializerOptions);

            var plan = await _planner.BuildPlanAsync(intents, ct).ConfigureAwait(false);
            record.PlanJson = JsonSerializer.Serialize(plan, SerializerOptions);

            var patch = await _roadmapPatcher.GeneratePatchAsync(requestText, intents, plan, ct).ConfigureAwait(false);
            record.PatchYaml = patch;

            var simulation = await _simulator.RunAsync(plan, patch, ct).ConfigureAwait(false);
            record.Status = simulation.IsSuccessful ? GenerationStatus.Simulated : GenerationStatus.Failed;

            var artifacts = await _artifactGenerator.GenerateAsync(plan, patch, simulation, ct).ConfigureAwait(false);
            record.ArtifactsSummary = JsonSerializer.Serialize(artifacts, SerializerOptions);

            if (simulation.IsSuccessful)
            {
                record.Status = GenerationStatus.Applied;
            }

            var result = new GenerationResult
            {
                Success = simulation.IsSuccessful,
                Plan = plan,
                PatchYaml = patch,
                Simulation = simulation,
                Artifacts = artifacts,
            };

            foreach (var warning in simulation.Warnings)
            {
                result.Warnings.Add(warning);
            }

            foreach (var warning in artifacts.Warnings)
            {
                result.Warnings.Add(warning);
            }

            await _auditTrail.RecordAsync(record, ct).ConfigureAwait(false);
            _logger.LogInformation("✅ AI generation completed. Success: {Success}", result.Success);

            return result;
        }
        catch (Exception ex)
        {
            record.Status = GenerationStatus.Failed;
            record.ErrorMessage = ex.ToString();

            try
            {
                await _auditTrail.RecordAsync(record, ct).ConfigureAwait(false);
            }
            catch (Exception auditEx)
            {
                _logger.LogError(auditEx, "❌ Failed to record audit trail after error");
            }

            _logger.LogError(ex, "❌ AI generation failed");
            throw;
        }
    }

}
