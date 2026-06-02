using System.Security.Claims;
using Zogreo.Api.Data;

namespace Zogreo.Api.Common.Tenancy;

// TODO (Slice 1): resolve org from JWT "org" claim → X-Org-Slug header → default org via DB lookup.
// For now, just sets a zero GUID so the scaffold compiles.
public class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx, ITenantProvider tenant, AppDbContext db)
    {
        // Skip webhook paths — they resolve their own tenant from the payment row.
        if (ctx.Request.Path.StartsWithSegments("/webhooks"))
        {
            await next(ctx);
            return;
        }

        // TODO: implement full resolution in Slice 1
        tenant.SetTenant(Guid.Empty, null, null);

        await next(ctx);
    }
}
