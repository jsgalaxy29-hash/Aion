using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aion.AI.Abstractions;
using Aion.AI.Models;

namespace Aion.AI.Services;

/// <summary>
/// Produces opinionated YAML patches compliant with the Aion naming conventions.
/// </summary>
public sealed class YamlRoadmapPatcher : IRoadmapPatcher
{
    public Task<string> GeneratePatchAsync(string requestText, IntentRecognitionResult intents, GenerationPlan plan, CancellationToken ct = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("modules:");
        builder.AppendLine($"  - name: {plan.ModuleName}");
        builder.AppendLine("    description: Module généré par le moteur IA");

        if (intents.Intents.Any(i => i.TargetEntities.Contains("Contrat")))
        {
            AppendContratModule(builder);
        }
        else
        {
            builder.AppendLine("    tables: []");
        }

        return Task.FromResult(builder.ToString());
    }

    private static void AppendContratModule(StringBuilder builder)
    {
        builder.AppendLine("    tables:");
        builder.AppendLine("      - name: FContrat");
        builder.AppendLine("        fields:");
        builder.AppendLine("          - name: Numero");
        builder.AppendLine("            type: string");
        builder.AppendLine("            maxLength: 50");
        builder.AppendLine("            isRequired: true");
        builder.AppendLine("            isUnique: true");
        builder.AppendLine("          - name: DateEffet");
        builder.AppendLine("            type: dateTimeOffset");
        builder.AppendLine("            isRequired: true");
        builder.AppendLine("          - name: DateEcheance");
        builder.AppendLine("            type: dateTimeOffset");
        builder.AppendLine("          - name: PrimeAnnuelle");
        builder.AppendLine("            type: decimal");
        builder.AppendLine("            precision: 18");
        builder.AppendLine("            scale: 2");
        builder.AppendLine("          - name: Statut");
        builder.AppendLine("            type: enum");
        builder.AppendLine("            values: [Brouillon, Actif, Resilie]");
        builder.AppendLine("      - name: FAssure");
        builder.AppendLine("        fields:");
        builder.AppendLine("          - name: Nom");
        builder.AppendLine("            type: string");
        builder.AppendLine("            maxLength: 200");
        builder.AppendLine("            isRequired: true");
        builder.AppendLine("          - name: Prenom");
        builder.AppendLine("            type: string");
        builder.AppendLine("            maxLength: 200");
        builder.AppendLine("            isRequired: true");
        builder.AppendLine("          - name: DateNaissance");
        builder.AppendLine("            type: dateTimeOffset");
        builder.AppendLine("          - name: Email");
        builder.AppendLine("            type: string");
        builder.AppendLine("            maxLength: 200");
        builder.AppendLine("            isUnique: true");
        builder.AppendLine("        relations:");
        builder.AppendLine("          - type: many-to-one");
        builder.AppendLine("            from: FAssure");
        builder.AppendLine("            to: FContrat");
        builder.AppendLine("            foreignKey: ContratId");
        builder.AppendLine("    actions:");
        builder.AppendLine("      - name: ValiderContrat");
        builder.AppendLine("        appliesTo: FContrat");
        builder.AppendLine("        allowedRoles: [Gestionnaire]");
    }
}
