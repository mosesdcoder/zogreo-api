using Zogreo.Domain.Common;

namespace Zogreo.Domain.Entities;

public class Intake : TenantEntity
{
    public Guid ProgramId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset OpensAt { get; set; }
    public DateTimeOffset ClosesAt { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public int? Capacity { get; set; }
    public bool Active { get; set; } = true;

    public Program Program { get; set; } = null!;
}
