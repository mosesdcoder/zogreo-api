using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Domain.Common;
using Zogreo.Domain.Entities;

namespace Zogreo.Infrastructure.Persistence.Interceptors;

public class AuditSaveChangesInterceptor(ITenantProvider tenant) : SaveChangesInterceptor
{
    private static readonly HashSet<string> SkipProps = ["PasswordHash", "RawPayload", "OrganizationId", "CreatedAt", "UpdatedAt"];

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (eventData.Context is null) return new ValueTask<InterceptionResult<int>>(result);
        StampAndAudit(eventData.Context);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    private void StampAndAudit(DbContext context)
    {
        var now = DateTimeOffset.UtcNow;
        var orgId = tenant.OrganizationId;
        var actorId = tenant.UserId;
        var auditRows = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries<TenantEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
                if (entry.Entity.OrganizationId == Guid.Empty)
                    entry.Entity.OrganizationId = orgId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }

            if (IsAudited(entry.Entity) && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                auditRows.Add(new AuditLog
                {
                    OrganizationId = entry.Entity.OrganizationId,
                    ActorUserId = actorId,
                    Action = entry.State.ToString(),
                    Entity = entry.Entity.GetType().Name,
                    EntityId = entry.Entity.Id,
                    Before = entry.State == EntityState.Added ? null : Snapshot(entry, false),
                    After = entry.State == EntityState.Deleted ? null : Snapshot(entry, true),
                    At = now
                });
            }
        }

        if (auditRows.Count > 0)
            context.Set<AuditLog>().AddRange(auditRows);
    }

    private static bool IsAudited(TenantEntity e) =>
        e is Domain.Entities.Application or Document or Offer or Invoice or Payment or Student or FeeType;

    private static string? Snapshot(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, bool current)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in entry.Properties)
        {
            if (SkipProps.Contains(prop.Metadata.Name)) continue;
            dict[prop.Metadata.Name] = current ? prop.CurrentValue : prop.OriginalValue;
        }
        return JsonSerializer.Serialize(dict);
    }
}
