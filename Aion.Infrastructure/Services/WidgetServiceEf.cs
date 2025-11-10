using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.Contracts;
using Aion.Domain.Widgets;
using Aion.DataEngine.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aion.Infrastructure.Services
{
    /// <summary>
    /// Service pour récupérer les widgets disponibles et charger les données via un résolveur.
    /// </summary>
    public sealed class WidgetServiceEf : IWidgetService
    {
        private readonly IDbContextFactory<AionDbContext> _dbFactory;
        private readonly IDataQueryResolver _resolver;

        public WidgetServiceEf(IDbContextFactory<AionDbContext> dbFactory, IDataQueryResolver resolver)
        {
            _dbFactory = dbFactory;
            _resolver = resolver;
        }

        public async Task<IReadOnlyList<WidgetEntity>> GetAvailableWidgetsAsync(int tenantId, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var widgets = await db.SWidget
                .AsNoTracking()
                .Where(w => w.TenantId == tenantId || w.TenantId == 0)
                .Select(w => new WidgetEntity
                {
                    Id = w.Id,
                    TenantId = w.TenantId,
                    Code = w.Code,
                    Title = w.Title,
                    Component = w.Component,
                    ConfigJson = w.ConfigJson,
                    IsActive = w.IsActive,
                    DataQueryRef = w.DataQueryRef
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return widgets;
        }

        public async Task<object?> GetDataAsync(string widgetCode, IDictionary<string, object?>? settings, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var widget = await db.SWidget.AsNoTracking().FirstOrDefaultAsync(w => w.Code == widgetCode, ct);
            if (widget is null) return null;
            return await _resolver.ExecuteAsync(widget.DataQueryRef, settings, ct);
        }
    }
}
