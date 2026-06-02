using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Infrastructure.Persistence;

namespace Zogreo.Api.Middleware;

public class TenantMiddleware(RequestDelegate next, IConfiguration config)
{
    public async Task InvokeAsync(HttpContext ctx, ITenantProvider tenant, ApplicationDbContext db)
    {
        if (ctx.Request.Path.StartsWithSegments("/webhooks"))
        {
            await next(ctx);
            return;
        }

        Guid orgId = Guid.Empty;

        var orgClaim = ctx.User.FindFirstValue("org");
        if (orgClaim != null && Guid.TryParse(orgClaim, out var fromJwt))
        {
            orgId = fromJwt;
        }
        else if (ctx.Request.Headers.TryGetValue("X-Org-Slug", out var slug) && !string.IsNullOrEmpty(slug))
        {
            var org = await db.Organizations.AsNoTracking().IgnoreQueryFilters()
                .FirstOrDefaultAsync(o => o.Slug == slug.ToString() && o.Active);
            if (org != null) orgId = org.Id;
        }

        if (orgId == Guid.Empty)
        {
            var defaultSlug = config["DefaultOrganization:Slug"] ?? "zogreo";
            var org = await db.Organizations.AsNoTracking().IgnoreQueryFilters()
                .FirstOrDefaultAsync(o => o.Slug == defaultSlug && o.Active);
            if (org != null) orgId = org.Id;
        }

        Guid? userId = null;
        var userIdClaim = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim, out var uid)) userId = uid;

        tenant.SetTenant(orgId, userId, ctx.User.FindFirstValue(ClaimTypes.Role));
        await next(ctx);
    }
}
