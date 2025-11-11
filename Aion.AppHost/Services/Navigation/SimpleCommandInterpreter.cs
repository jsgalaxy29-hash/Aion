using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.UI.Navigation;

namespace Aion.AppHost.Services.Navigation;

/// <summary>
/// Baseline implementation that recognises a small set of French navigation commands.
/// </summary>
public sealed class SimpleCommandInterpreter : ICommandInterpreter
{
    private static readonly string[] OpenVerbs = { "ouvre", "ouvrir", "lance", "affiche" };
    private static readonly string[] CloseVerbs = { "ferme", "fermer" };
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "le", "la", "les", "l", "l'", "module", "modules", "du", "de", "des", "mon", "ma", "mes"
    };

    public async Task<NavigationCommand?> TryInterpretAsync(string input, IModuleCatalog catalog, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var normalized = Normalize(input);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.Contains("ferme tout", StringComparison.Ordinal) || normalized.Contains("ferme tous les modules", StringComparison.Ordinal))
        {
            return new NavigationCommand(NavigationCommandType.CloseAll, null);
        }

        if (TryExtractCommand(normalized, OpenVerbs, out var candidate))
        {
            var module = await catalog.TryMatchAsync(candidate, ct).ConfigureAwait(false);
            return module is null ? null : new NavigationCommand(NavigationCommandType.OpenModule, module);
        }

        if (TryExtractCommand(normalized, CloseVerbs, out candidate))
        {
            var module = await catalog.TryMatchAsync(candidate, ct).ConfigureAwait(false);
            return module is null ? null : new NavigationCommand(NavigationCommandType.CloseModule, module);
        }

        return null;
    }

    private static bool TryExtractCommand(string normalized, IEnumerable<string> verbs, out string candidate)
    {
        foreach (var verb in verbs)
        {
            if (!normalized.StartsWith(verb, StringComparison.Ordinal))
            {
                continue;
            }

            var remainder = normalized[verb.Length..];
            remainder = remainder.Trim();
            if (string.IsNullOrWhiteSpace(remainder))
            {
                continue;
            }

            candidate = CleanupCandidate(remainder);
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return true;
            }
        }

        candidate = string.Empty;
        return false;
    }

    private static string CleanupCandidate(string text)
    {
        var tokens = text
            .Replace("'", " ", StringComparison.Ordinal)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(token => new string(token.Where(char.IsLetterOrDigit).ToArray()))
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Where(token => !StopWords.Contains(token))
            .ToArray();

        return string.Join(' ', tokens).Trim();
    }

    private static string Normalize(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString().Trim();
    }
}
