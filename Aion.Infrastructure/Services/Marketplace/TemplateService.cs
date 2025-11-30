using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Aion.DataEngine.Entities;
using Aion.DataEngine.Interfaces;
using Aion.Domain.Contracts;
using Aion.Domain.Marketplace;
using Microsoft.EntityFrameworkCore;

namespace Aion.Infrastructure.Services.Marketplace;

/// <summary>
/// Provides import/export capabilities for modules shared through the marketplace.
/// </summary>
public sealed class TemplateService : ITemplateService
{
    private static readonly JsonSerializerOptions SignatureSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly AionDbContext _dbContext;
    private readonly IUserContext _userContext;

    public TemplateService(AionDbContext dbContext, IUserContext userContext)
    {
        _dbContext = dbContext;
        _userContext = userContext;
    }

    public async Task<ModuleTemplate> ExportModuleAsync(int moduleId, CancellationToken cancellationToken = default)
    {
        var module = await _dbContext.SModule.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken)
            .ConfigureAwait(false)
                     ?? throw new InvalidOperationException($"Module {moduleId} introuvable.");

        var menus = await _dbContext.SMenu.AsNoTracking()
            .Where(m => m.ModuleId == moduleId)
            .OrderBy(m => m.Order)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var parentLabels = await LoadParentLabelsAsync(menus, cancellationToken).ConfigureAwait(false);
        var tableNames = ResolveTableNames(module, menus);
        var tables = tableNames.Count == 0
            ? new List<STable>()
            : await _dbContext.STable.AsNoTracking()
                .Include(t => t.Champs)
                .Where(t => tableNames.Contains(t.Libelle))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        var template = new ModuleTemplate
        {
            Version = ModuleTemplate.CurrentVersion,
            ExportedAtUtc = DateTime.UtcNow,
            Module = new ModuleTemplateDescriptor
            {
                Name = module.Name,
                Description = module.Description,
                Route = module.Route,
                Icon = module.Icon,
                Order = module.Order
            },
            Menus = BuildMenuTemplates(menus, parentLabels),
            Tables = BuildTableTemplates(tables)
        };

        template.Signature = ComputeSignature(template);
        return template;
    }

    public async Task<int> ImportModuleAsync(ModuleTemplate template, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);

        ValidateTemplate(template);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var module = new SModule
        {
            Name = template.Module.Name,
            Description = string.IsNullOrWhiteSpace(template.Module.Description)
                ? template.Module.Name
                : template.Module.Description!,
            Route = template.Module.Route,
            Icon = template.Module.Icon,
            Order = template.Module.Order,
            TenantId = _userContext.TenantId,
            DtCreation = now
        };

        _dbContext.SModule.Add(module);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var menuLookup = await EnsureParentMenusAsync(template.Menus, now, cancellationToken).ConfigureAwait(false);
        var menuQueue = new Queue<MenuTemplate>(template.Menus.OrderBy(m => m.Order));
        var safetyCounter = 0;
        var safetyThreshold = Math.Max(1, menuQueue.Count * 2);

        while (menuQueue.Count > 0)
        {
            var menuTemplate = menuQueue.Dequeue();
            var parentId = ResolveParentId(menuTemplate.ParentLabel, menuLookup);

            if (!string.IsNullOrWhiteSpace(menuTemplate.ParentLabel) && parentId is null)
            {
                menuQueue.Enqueue(menuTemplate);
                safetyCounter++;

                if (safetyCounter > safetyThreshold)
                {
                    throw new InvalidOperationException($"Impossible de résoudre le parent '{menuTemplate.ParentLabel}' pour le menu '{menuTemplate.Label}'.");
                }

                continue;
            }

            var menu = new SMenu
            {
                ModuleId = module.Id,
                Libelle = string.IsNullOrWhiteSpace(menuTemplate.Label) ? module.Name : menuTemplate.Label,
                ParentId = parentId,
                Icon = menuTemplate.Icon,
                IsLeaf = menuTemplate.IsLeaf,
                Order = menuTemplate.Order,
                Parametre = menuTemplate.Parameters,
                TenantId = _userContext.TenantId,
                DtCreation = now
            };

            _dbContext.SMenu.Add(menu);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            menuLookup[menu.Libelle] = menu.Id;
            safetyCounter = 0;
        }

        foreach (var table in template.Tables)
        {
            var sTable = new STable
            {
                Libelle = table.Name,
                Description = table.Description,
                Parent = table.Parent,
                ParentLiaison = table.ParentLink,
                ReferentielLibelle = table.ReferentielLibelle,
                Type = table.Type,
                IsHistorise = table.IsHistorise,
                TenantId = _userContext.TenantId,
                DtCreation = now
            };

            _dbContext.STable.Add(sTable);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            foreach (var field in table.Fields)
            {
                var sField = new SField
                {
                    TableId = sTable.Id,
                    Libelle = field.Name,
                    Alias = field.Alias,
                    DataType = field.DataType,
                    IsClePrimaire = field.IsPrimaryKey,
                    IsUnique = field.IsUnique,
                    Taille = field.Length ?? 0,
                    Referentiel = field.Referentiel,
                    ReferentielWhereClause = field.ReferentielWhereClause,
                    Defaut = field.DefaultValue,
                    IsNulleable = field.IsNullable,
                    Min = field.Min,
                    Max = field.Max,
                    IsVisible = field.IsVisible,
                    Ordre = field.Order,
                    Format = field.Format,
                    Masque = field.Mask,
                    IsLinkToBdd = true,
                    IsSearch = field.IsSearch,
                    SearchOperator = field.SearchOperator,
                    SearchDefautValue = field.SearchDefaultValue,
                    CoordonneeX = field.CoordinateX,
                    CoordonneeY = field.CoordinateY,
                    CoordonneeLabelX = field.CoordinateLabelX,
                    CoordonneeLabelY = field.CoordinateLabelY,
                    IsHistorise = field.IsHistorise,
                    Commentaire = field.Comment,
                    ValidationScript = field.ValidationScript,
                    ValidationYaml = field.ValidationYaml,
                    TenantId = _userContext.TenantId,
                    DtCreation = now
                };

                _dbContext.SField.Add(sField);
            }

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return module.Id;
    }

    private static IList<MenuTemplate> BuildMenuTemplates(IReadOnlyCollection<SMenu> menus, IReadOnlyDictionary<int, string> externalParents)
    {
        if (menus.Count == 0)
        {
            return Array.Empty<MenuTemplate>();
        }

        return menus
            .Select(menu => new MenuTemplate
            {
                Label = menu.Libelle,
                ParentLabel = ResolveParentLabel(menu, menus, externalParents),
                Icon = menu.Icon,
                IsLeaf = menu.IsLeaf,
                Order = menu.Order,
                Parameters = menu.Parametre
            })
            .ToList();
    }

    private static IList<TableTemplate> BuildTableTemplates(IEnumerable<STable> tables)
    {
        return tables
            .Select(table => new TableTemplate
            {
                Name = table.Libelle,
                Description = table.Description,
                Parent = table.Parent,
                ParentLink = table.ParentLiaison,
                ReferentielLibelle = table.ReferentielLibelle,
                Type = table.Type,
                IsHistorise = table.IsHistorise,
                Fields = table.Champs
                    .Select(field => new FieldTemplate
                    {
                        Name = field.Libelle,
                        Alias = field.Alias,
                        DataType = field.DataType,
                        IsPrimaryKey = field.IsClePrimaire,
                        IsUnique = field.IsUnique,
                        IsNullable = field.IsNulleable,
                        Length = field.Taille == 0 ? null : field.Taille,
                        Referentiel = field.Referentiel,
                        ReferentielWhereClause = field.ReferentielWhereClause,
                        DefaultValue = field.Defaut,
                        Min = field.Min,
                        Max = field.Max,
                        IsVisible = field.IsVisible,
                        Order = field.Ordre,
                        Format = field.Format,
                        Mask = field.Masque,
                        IsSearch = field.IsSearch,
                        SearchOperator = field.SearchOperator,
                        SearchDefaultValue = field.SearchDefautValue,
                        CoordinateX = field.CoordonneeX,
                        CoordinateY = field.CoordonneeY,
                        CoordinateLabelX = field.CoordonneeLabelX,
                        CoordinateLabelY = field.CoordonneeLabelY,
                        IsHistorise = field.IsHistorise,
                        Comment = field.Commentaire,
                        ValidationScript = field.ValidationScript,
                        ValidationYaml = field.ValidationYaml
                    })
                    .ToList()
            })
            .ToList();
    }

    private async Task<IReadOnlyDictionary<int, string>> LoadParentLabelsAsync(IEnumerable<SMenu> menus, CancellationToken ct)
    {
        var parentIds = menus
            .Where(m => m.ParentId.HasValue)
            .Select(m => m.ParentId!.Value)
            .Except(menus.Select(m => m.Id))
            .Distinct()
            .ToList();

        if (parentIds.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        var parents = await _dbContext.SMenu.AsNoTracking()
            .Where(m => parentIds.Contains(m.Id))
            .Select(m => new { m.Id, m.Libelle })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return parents.ToDictionary(p => p.Id, p => p.Libelle);
    }

    private static string? ResolveParentLabel(SMenu menu, IReadOnlyCollection<SMenu> moduleMenus, IReadOnlyDictionary<int, string> externalParents)
    {
        if (!menu.ParentId.HasValue)
        {
            return null;
        }

        var parent = moduleMenus.FirstOrDefault(m => m.Id == menu.ParentId.Value);
        if (parent is not null)
        {
            return parent.Libelle;
        }

        return externalParents.TryGetValue(menu.ParentId.Value, out var label) ? label : null;
    }

    private async Task<Dictionary<string, int>> EnsureParentMenusAsync(IEnumerable<MenuTemplate> menus, DateTime now, CancellationToken ct)
    {
        var internalLabels = new HashSet<string>(menus.Select(m => m.Label), StringComparer.OrdinalIgnoreCase);
        var requestedParents = menus
            .Where(m => !string.IsNullOrWhiteSpace(m.ParentLabel))
            .Select(m => m.ParentLabel!)
            .Where(label => !internalLabels.Contains(label))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (requestedParents.Count == 0)
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        var existingParents = await _dbContext.SMenu.AsNoTracking()
            .Where(m => requestedParents.Contains(m.Libelle))
            .Select(m => new { m.Libelle, m.Id })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var lookup = existingParents.ToDictionary(x => x.Libelle, x => x.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var parentLabel in requestedParents)
        {
            if (lookup.ContainsKey(parentLabel))
            {
                continue;
            }

            var parentMenu = new SMenu
            {
                Libelle = parentLabel,
                IsLeaf = false,
                Order = 1000,
                TenantId = _userContext.TenantId,
                DtCreation = now
            };

            _dbContext.SMenu.Add(parentMenu);
            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            lookup[parentLabel] = parentMenu.Id;
        }

        return lookup;
    }

    private static int? ResolveParentId(string? parentLabel, IReadOnlyDictionary<string, int> lookup)
    {
        if (string.IsNullOrWhiteSpace(parentLabel))
        {
            return null;
        }

        return lookup.TryGetValue(parentLabel, out var id) ? id : null;
    }

    private static HashSet<string> ResolveTableNames(SModule module, IReadOnlyCollection<SMenu> menus)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var menu in menus)
        {
            if (string.IsNullOrWhiteSpace(menu.Parametre))
            {
                continue;
            }

            var parameters = menu.Parametre.Split(new[] { '&', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var parameter in parameters)
            {
                var parts = parameter.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && parts[0].Trim().Equals("TableName", StringComparison.OrdinalIgnoreCase))
                {
                    names.Add(Uri.UnescapeDataString(parts[1].Trim()));
                }
            }
        }

        if (names.Count == 0 && !string.IsNullOrWhiteSpace(module.Route))
        {
            var segments = module.Route.Trim('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0)
            {
                names.Add(Uri.UnescapeDataString(segments[^1]));
            }
        }

        return names;
    }

    private static void ValidateTemplate(ModuleTemplate template)
    {
        if (!string.Equals(template.Version, ModuleTemplate.CurrentVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Version du template non supportée : {template.Version}. Attendu : {ModuleTemplate.CurrentVersion}.");
        }

        var expectedSignature = ComputeSignature(template);
        if (!string.Equals(expectedSignature, template.Signature, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Signature du template invalide ou corrompue.");
        }
    }

    private static string ComputeSignature(ModuleTemplate template)
    {
        var payload = new TemplateSignaturePayload
        {
            Version = template.Version,
            ExportedAtUtc = template.ExportedAtUtc,
            Module = template.Module,
            Menus = template.Menus,
            Tables = template.Tables
        };

        var json = JsonSerializer.Serialize(payload, SignatureSerializerOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private sealed class TemplateSignaturePayload
    {
        public string Version { get; set; } = ModuleTemplate.CurrentVersion;

        public DateTime ExportedAtUtc { get; set; }

        public ModuleTemplateDescriptor Module { get; set; } = new();

        public IList<MenuTemplate> Menus { get; set; } = new List<MenuTemplate>();

        public IList<TableTemplate> Tables { get; set; } = new List<TableTemplate>();
    }
}
