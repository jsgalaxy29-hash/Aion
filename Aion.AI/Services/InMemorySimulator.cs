using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Abstractions;
using Aion.AI.Models;

namespace Aion.AI.Services;

/// <summary>
/// Performs lightweight validation without touching the database.
/// </summary>
public sealed class InMemorySimulator : ISimulator
{
    public Task<SimulationResult> RunAsync(GenerationPlan plan, string patchYaml, CancellationToken ct = default)
    {
        var result = new SimulationResult
        {
            IsSuccessful = true
        };

        var lines = patchYaml.Split('\n');
        foreach (var line in lines)
        {
            if (!line.TrimStart().StartsWith("name:"))
            {
                continue;
            }

            var name = line.Split(':').ElementAtOrDefault(1)?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (name.Contains('_'))
            {
                result.IsSuccessful = false;
                result.Errors.Add($"Le nom '{name}' contient un caractère '_' interdit.");
            }
        }

        if (!plan.Steps.Any())
        {
            result.IsSuccessful = false;
            result.Errors.Add("Le plan de génération est vide.");
        }

        return Task.FromResult(result);
    }
}
