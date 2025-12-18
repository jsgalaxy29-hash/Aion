using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Abstractions;
using Aion.AI.Models;

namespace Aion.AI.Services;

/// <summary>
/// Simple rule-based intent recognizer to bootstrap the AI pipeline.
/// </summary>
public sealed class RuleBasedIntentRecognizer : IIntentRecognizer
{
    public Task<IntentRecognitionResult> RecognizeAsync(string requestText, CancellationToken ct = default)
    {
        var normalized = requestText.ToLower(CultureInfo.InvariantCulture);
        var intents = new List<DetectedIntent>();

        if (normalized.Contains("module") || normalized.Contains("contrat"))
        {
            intents.Add(new DetectedIntent
            {
                Type = RecognizedIntentType.CreateModule,
                TargetModule = ExtractModuleName(requestText),
                TargetEntities = new List<string>()
            });
        }

        if (normalized.Contains("crud"))
        {
            intents.Add(new DetectedIntent
            {
                Type = RecognizedIntentType.GenerateCrud,
                TargetModule = ExtractModuleName(requestText),
                TargetEntities = ExtractEntityNames(requestText)
            });
        }

        if (!intents.Any())
        {
            intents.Add(new DetectedIntent { Type = RecognizedIntentType.Unknown });
        }

        var result = new IntentRecognitionResult
        {
            Intents = intents,
            Metadata = new Dictionary<string, string>
            {
                ["module"] = ExtractModuleName(requestText)
            }
        };

        return Task.FromResult(result);
    }

    private static string ExtractModuleName(string text)
    {
        var parts = text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var moduleKeywords = new[] { "module", "modules" };
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (moduleKeywords.Contains(parts[i], StringComparer.OrdinalIgnoreCase))
            {
                return parts[i + 1].Trim(',', '.', ';');
            }
        }

        return "Generic";
    }

    private static IReadOnlyList<string> ExtractEntityNames(string text)
    {
        var entities = new List<string>();
        if (text.Contains("Contrat", StringComparison.OrdinalIgnoreCase))
        {
            entities.Add("Contrat");
        }

        if (text.Contains("Assure", StringComparison.OrdinalIgnoreCase) || text.Contains("Assuré", StringComparison.OrdinalIgnoreCase))
        {
            entities.Add("Assure");
        }

        return entities;
    }
}
