using Zogreo.Domain.Common;
using Zogreo.Domain.Enums;

namespace Zogreo.Domain.Entities;

public class Program : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public ProgramLevel Level { get; set; }
    public DeliveryMode Mode { get; set; }
    public string DurationLabel { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Active { get; set; } = true;

    public ICollection<Intake> Intakes { get; set; } = new List<Intake>();
}
