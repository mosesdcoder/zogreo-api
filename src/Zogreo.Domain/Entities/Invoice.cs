using Zogreo.Domain.Common;
using Zogreo.Domain.Enums;

namespace Zogreo.Domain.Entities;

public class Invoice : TenantEntity
{
    public Guid ApplicationId { get; set; }
    public Guid FeeTypeId { get; set; }
    public FeeCode FeeCode { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountPaid { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
    public DateTimeOffset? DueAt { get; set; }

    public Application Application { get; set; } = null!;
    public FeeType FeeType { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
