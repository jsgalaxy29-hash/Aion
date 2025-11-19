using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.Contracts;
using Aion.Domain.ModuleBuilder;

namespace Aion.AI.Services;

/// <summary>
/// Mock implementation that converts a JSON-like prompt into an in-memory blueprint.
/// It allows administrators to paste a structured payload while waiting for a real LLM provider.
/// </summary>
public sealed class MockAiModuleSpecGenerator : IAiModuleSpecGenerator
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public Task<AiModuleBlueprint> GenerateAsync(string naturalLanguagePrompt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(naturalLanguagePrompt);

        ModuleSpec? spec = null;
        try
        {
            spec = JsonSerializer.Deserialize<ModuleSpec>(naturalLanguagePrompt, SerializerOptions);
        }
        catch (JsonException)
        {
            // Fallback to a minimal scaffold if the prompt is not valid JSON.
        }

        spec ??= new ModuleSpec
        {
            Name = "Module généré",
            Description = "Gabarit issu du prompt fourni",
            Tables = new List<TableSpec>()
        };

        var blueprint = new AiModuleBlueprint
        {
            Name = string.IsNullOrWhiteSpace(spec.Name) ? "Module généré" : spec.Name!,
            Description = spec.Description,
            NaturalLanguagePrompt = naturalLanguagePrompt,
            ParsedSpecificationJson = JsonSerializer.Serialize(spec, SerializerOptions),
            Status = "Draft",
            Tables = spec.Tables?.Select(MapTable).ToList() ?? new List<AiTableBlueprint>()
        };

        return Task.FromResult(blueprint);
    }

    private static AiTableBlueprint MapTable(TableSpec table)
    {
        var technicalName = string.IsNullOrWhiteSpace(table.TechnicalName)
            ? table.DisplayName ?? table.Name ?? "Table"
            : table.TechnicalName;

        var displayName = string.IsNullOrWhiteSpace(table.DisplayName)
            ? table.Name ?? table.TechnicalName ?? "Table"
            : table.DisplayName;

        return new AiTableBlueprint
        {
            TechnicalName = technicalName!,
            DisplayName = displayName!,
            Description = table.Description,
            Fields = table.Fields?.Select(MapField).ToList() ?? new List<AiFieldBlueprint>()
        };
    }

    private static AiFieldBlueprint MapField(FieldSpec field)
    {
        var technicalName = string.IsNullOrWhiteSpace(field.TechnicalName)
            ? field.DisplayName ?? field.Name ?? "Field"
            : field.TechnicalName;

        var displayName = string.IsNullOrWhiteSpace(field.DisplayName)
            ? field.Name ?? field.TechnicalName ?? "Field"
            : field.DisplayName;

        return new AiFieldBlueprint
        {
            TechnicalName = technicalName!,
            DisplayName = displayName!,
            DataType = string.IsNullOrWhiteSpace(field.DataType) ? "string" : field.DataType!,
            IsRequired = field.IsRequired,
            IsPrimaryKey = field.IsPrimaryKey,
            IsUnique = field.IsUnique,
            MaxLength = field.MaxLength,
            ForeignKeyTargetTable = field.ForeignKeyTargetTable,
            ForeignKeyTargetField = field.ForeignKeyTargetField
        };
    }

    private sealed class ModuleSpec
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public IList<TableSpec>? Tables { get; set; }
    }

    private sealed class TableSpec
    {
        public string? Name { get; set; }
        public string? TechnicalName { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public IList<FieldSpec>? Fields { get; set; }
    }

    private sealed class FieldSpec
    {
        public string? Name { get; set; }
        public string? TechnicalName { get; set; }
        public string? DisplayName { get; set; }
        public string? DataType { get; set; }
        public bool IsRequired { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsUnique { get; set; }
        public int? MaxLength { get; set; }
        public string? ForeignKeyTargetTable { get; set; }
        public string? ForeignKeyTargetField { get; set; }
    }
}
