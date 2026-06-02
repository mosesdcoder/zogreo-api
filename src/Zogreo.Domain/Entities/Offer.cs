using Zogreo.Domain.Common;
using Zogreo.Domain.Enums;

namespace Zogreo.Domain.Entities;

public class Offer : TenantEntity
{
    public Guid ApplicationId { get; set; }
    public OfferStatus Status { get; set; } = OfferStatus.Issued;
    public string? LetterUrl { get; set; }
    public string? Conditions { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }

    public Application Application { get; set; } = null!;
}
