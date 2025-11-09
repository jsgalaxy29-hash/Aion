using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Aion.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Aion.AppHost.Workers;

internal static class TenantExecutionHelper
{
    public static async Task<List<int>> GetTenantIdsAsync(IDbContextFactory<AionDbContext> dbContextFactory, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var tenantIds = await db.STenant
            .IgnoreQueryFilters()
            .Select(t => t.TenantId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (tenantIds.Count == 0)
        {
            tenantIds.Add(1);
        }

        return tenantIds;
    }

    public static HttpContext CreateBackgroundHttpContext(int tenantId)
    {
        var identity = new ClaimsIdentity("BackgroundWorker");
        identity.AddClaim(new Claim("tenant", tenantId.ToString(CultureInfo.InvariantCulture)));
        identity.AddClaim(new Claim("sub", "0"));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "0"));
        identity.AddClaim(new Claim(ClaimTypes.Name, "System"));

        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
    }
}
