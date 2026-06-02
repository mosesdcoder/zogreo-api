using Zogreo.Domain.Common;
using Zogreo.Domain.Enums;

namespace Zogreo.Domain.Entities;

public class Payment : TenantEntity
{
    public Guid InvoiceId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Provider { get; set; } = "paystack";
    public PaymentChannel Channel { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public decimal AmountGross { get; set; }
    public decimal ProviderFee { get; set; }
    public decimal TechnologyFee { get; set; }
    public decimal AmountNetToSchool { get; set; }
    public string? ProviderRef { get; set; }
    public string? AuthorizationUrl { get; set; }
    public string? RawPayload { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public Invoice Invoice { get; set; } = null!;
}
