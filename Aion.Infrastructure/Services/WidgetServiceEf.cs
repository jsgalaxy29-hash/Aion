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
        private readonly AionDbContext _db;
        private readonly IDataQueryResolver _resolver;

        public WidgetServiceEf(AionDbContext db, IDataQueryResolver resolver)
        {
            _db = db;
            _resolver = resolver;
        }

        public async Task<IReadOnlyList<WidgetEntity>> GetAvailableWidgetsAsync(int tenantId, CancellationToken ct)
        {
            return (IReadOnlyList<WidgetEntity>)await _db.SWidget.Where(w => w.TenantId == tenantId || w.TenantId == 0).ToListAsync(ct);
        }

        public async Task<object?> GetDataAsync(string widgetCode, IDictionary<string, object?>? settings, CancellationToken ct)
        {
            var widget = await _db.SWidget.FirstOrDefaultAsync(w => w.Code == widgetCode, ct);
            if (widget is null) return null;
            return await _resolver.ExecuteAsync(widget.DataQueryRef, settings, ct);
        }
    }
}
