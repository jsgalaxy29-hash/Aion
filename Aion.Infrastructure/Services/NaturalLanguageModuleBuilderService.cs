using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aion.DataEngine.Entities;
using Aion.Domain.Contracts;
using Aion.Domain.ModuleBuilder;
using Microsoft.EntityFrameworkCore;

namespace Aion.Infrastructure.Services;

/// <summary>
/// Basic implementation that projects a validated blueprint into the system catalog tables.
/// </summary>
public class NaturalLanguageModuleBuilderService : INaturalLanguageModuleBuilder
{
    private readonly AionDbContext _dbContext;

    public NaturalLanguageModuleBuilderService(AionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task BuildAsync(AiModuleBlueprint blueprint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(blueprint);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (_dbContext.Entry(blueprint).State == EntityState.Detached)
        {
            _dbContext.AiModuleBlueprints.Add(blueprint);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var module = new SModule
        {
            Name = blueprint.Name,
            Description = blueprint.Description ?? blueprint.Name,
            Route = "/modules/ai-designer",
            Icon = "SparkleFilled"
        };

        _dbContext.SModule.Add(module);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var table in blueprint.Tables)
        {
            var sTable = new STable
            {
                Libelle = string.IsNullOrWhiteSpace(table.TechnicalName) ? table.DisplayName : table.TechnicalName,
                Description = table.Description ?? table.DisplayName,
                Type = "F",
                Actif = true,
                IsHistorise = true
            };

            _dbContext.STable.Add(sTable);
            await _dbContext.SaveChangesAsync(cancellationToken);

            foreach (var field in table.Fields)
            {
                var sField = new SField
                {
                    TableId = sTable.Id,
                    Libelle = field.TechnicalName,
                    Alias = field.DisplayName,
                    DataType = NormalizeDataType(field.DataType),
                    IsClePrimaire = field.IsPrimaryKey,
                    IsUnique = field.IsUnique,
                    Taille = field.MaxLength ?? 0,
                    IsNulleable = !field.IsRequired,
                    Referentiel = field.ForeignKeyTargetTable,
                    //ReferentielLibelle = field.ForeignKeyTargetField,
                    IsVisible = true,
                    IsLinkToBdd = true
                };

                _dbContext.SField.Add(sField);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static string NormalizeDataType(string dataType)
    {
        if (string.IsNullOrWhiteSpace(dataType))
        {
            return "NVARCHAR";
        }

        var normalized = dataType.Trim().ToUpperInvariant();

        return normalized switch
        {
            "STRING" => "NVARCHAR",
            "INT" => "INT",
            "INTEGER" => "INT",
            "DECIMAL" => "DECIMAL",
            "DATE" => "DATE",
            "DATETIME" => "DATETIME",
            "BOOL" => "BIT",
            "BOOLEAN" => "BIT",
            _ => normalized
        };
    }
}
