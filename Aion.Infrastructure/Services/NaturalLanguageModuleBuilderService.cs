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

        var primaryTable = blueprint.Tables.FirstOrDefault();
        var primaryTableName = primaryTable?.DisplayName ?? primaryTable?.TechnicalName ?? blueprint.Name;
        var now = DateTime.UtcNow;

        var module = new SModule
        {
            Name = blueprint.Name,
            Description = blueprint.Description ?? blueprint.Name,
            Route = $"/dynamic/list/{Uri.EscapeDataString(primaryTableName)}",
            Icon = "SparkleFilled",
            Order = 980,
            DtCreation = now
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
                    IsLinkToBdd = true,
                    DtCreation = now
                };

                _dbContext.SField.Add(sField);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await EnsureMenuAndRightsAsync(module, primaryTableName, now, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task EnsureMenuAndRightsAsync(SModule module, string primaryTableName, DateTime now, CancellationToken cancellationToken)
    {
        var adminRoot = await _dbContext.SMenu.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Libelle == "Administration", cancellationToken)
            .ConfigureAwait(false);

        if (adminRoot is null)
        {
            adminRoot = new SMenu
            {
                Libelle = "Administration",
                IsLeaf = false,
                Icon = "Settings20Regular",
                Order = 900,
                DtCreation = now
            };

            _dbContext.SMenu.Add(adminRoot);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var moduleMenu = new SMenu
        {
            ModuleId = module.Id,
            Libelle = module.Name,
            ParentId = adminRoot.Id,
            Icon = module.Icon,
            IsLeaf = true,
            Order = adminRoot.Order + 5,
            Parametre = $"TableName={primaryTableName}",
            DtCreation = now
        };

        _dbContext.SMenu.Add(moduleMenu);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var adminGroup = await _dbContext.SGroup.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Name == "Administrateurs" || g.Name == "Administrateur", cancellationToken)
            .ConfigureAwait(false);

        if (adminGroup is null)
        {
            return;
        }

        var hasRight = await _dbContext.SRight.IgnoreQueryFilters()
            .AnyAsync(r => r.GroupId == adminGroup.Id && r.Target == "Menu" && r.SubjectId == moduleMenu.Id, cancellationToken)
            .ConfigureAwait(false);

        if (!hasRight)
        {
            _dbContext.SRight.Add(new SRight
            {
                GroupId = adminGroup.Id,
                Target = "Menu",
                SubjectId = moduleMenu.Id,
                Right1 = true,
                TenantId = module.TenantId,
                Actif = true,
                Doc = false,
                DtCreation = now
            });

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
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
