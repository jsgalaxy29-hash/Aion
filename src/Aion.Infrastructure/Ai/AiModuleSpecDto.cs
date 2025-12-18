using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Aion.Domain.ModuleBuilder;

namespace Aion.Infrastructure.Ai;

internal sealed class AiModuleSpecDto
{
    [JsonPropertyName("moduleName")]
    public string? ModuleName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("tables")]
    public IList<AiTableSpecDto>? Tables { get; set; }
}

internal sealed class AiTableSpecDto
{
    [JsonPropertyName("technicalName")]
    public string? TechnicalName { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("fields")]
    public IList<AiFieldSpecDto>? Fields { get; set; }
}

internal sealed class AiFieldSpecDto
{
    [JsonPropertyName("technicalName")]
    public string? TechnicalName { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("dataType")]
    public string? DataType { get; set; }

    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; set; }

    [JsonPropertyName("isPrimaryKey")]
    public bool IsPrimaryKey { get; set; }

    [JsonPropertyName("isUnique")]
    public bool IsUnique { get; set; }

    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; set; }

    [JsonPropertyName("foreignKeyTargetTable")]
    public string? ForeignKeyTargetTable { get; set; }

    [JsonPropertyName("foreignKeyTargetField")]
    public string? ForeignKeyTargetField { get; set; }
}

internal static class AiModuleSpecMapping
{
    public static AiModuleBlueprint ToBlueprint(this AiModuleSpecDto dto, string naturalLanguagePrompt, string parsedJson)
    {
        var moduleName = string.IsNullOrWhiteSpace(dto.ModuleName) ? "Module généré" : dto.ModuleName!;
        var description = string.IsNullOrWhiteSpace(dto.Description) ? dto.ModuleName : dto.Description;

        return new AiModuleBlueprint
        {
            Name = moduleName,
            Description = description,
            NaturalLanguagePrompt = naturalLanguagePrompt,
            ParsedSpecificationJson = parsedJson,
            Status = "Draft",
            Tables = dto.Tables?.Select(MapTable).ToList() ?? new List<AiTableBlueprint>()
        };
    }

    private static AiTableBlueprint MapTable(AiTableSpecDto dto)
    {
        var technicalName = string.IsNullOrWhiteSpace(dto.TechnicalName)
            ? dto.DisplayName ?? "Table"
            : dto.TechnicalName;

        var displayName = string.IsNullOrWhiteSpace(dto.DisplayName)
            ? dto.TechnicalName ?? "Table"
            : dto.DisplayName;

        return new AiTableBlueprint
        {
            TechnicalName = technicalName!,
            DisplayName = displayName!,
            Description = dto.Description,
            Fields = dto.Fields?.Select(MapField).ToList() ?? new List<AiFieldBlueprint>()
        };
    }

    private static AiFieldBlueprint MapField(AiFieldSpecDto dto)
    {
        var technicalName = string.IsNullOrWhiteSpace(dto.TechnicalName)
            ? dto.DisplayName ?? "Field"
            : dto.TechnicalName;

        var displayName = string.IsNullOrWhiteSpace(dto.DisplayName)
            ? dto.TechnicalName ?? "Field"
            : dto.DisplayName;

        return new AiFieldBlueprint
        {
            TechnicalName = technicalName!,
            DisplayName = displayName!,
            DataType = string.IsNullOrWhiteSpace(dto.DataType) ? "string" : dto.DataType!,
            IsRequired = dto.IsRequired,
            IsPrimaryKey = dto.IsPrimaryKey,
            IsUnique = dto.IsUnique,
            MaxLength = dto.MaxLength,
            ForeignKeyTargetTable = dto.ForeignKeyTargetTable,
            ForeignKeyTargetField = dto.ForeignKeyTargetField
        };
    }
}
