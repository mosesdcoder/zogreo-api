namespace Zogreo.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? Before { get; set; }
    public string? After { get; set; }
    public DateTimeOffset At { get; set; }
}
