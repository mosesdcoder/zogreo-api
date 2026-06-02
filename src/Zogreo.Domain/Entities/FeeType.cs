using Zogreo.Domain.Common;
using Zogreo.Domain.Enums;

namespace Zogreo.Domain.Entities;

public class FeeType : TenantEntity
{
    public FeeCode Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool Refundable { get; set; }
    public bool Active { get; set; } = true;
}
