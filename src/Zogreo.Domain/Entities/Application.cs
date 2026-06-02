using Zogreo.Domain.Common;
using Zogreo.Domain.Enums;
using Zogreo.Domain.Exceptions;

namespace Zogreo.Domain.Entities;

public class Application : TenantEntity
{
    public Guid UserId { get; set; }
    public Guid ProgramId { get; set; }
    public Guid IntakeId { get; set; }
    public ApplicationStatus Status { get; private set; } = ApplicationStatus.Draft;
    public string? PersonalJson { get; set; }
    public string? EducationHistoryJson { get; set; }
    public string? NextOfKinJson { get; set; }
    public string? HowDidYouHear { get; set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public Guid? DecidedByUserId { get; set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public string? DecisionReason { get; set; }

    public User User { get; set; } = null!;
    public Program Program { get; set; } = null!;
    public Intake Intake { get; set; } = null!;
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public Offer? Offer { get; set; }
    public Student? Student { get; set; }

    // ── Guarded transition methods ────────────────────────────────────────────

    public void Submit()
    {
        Guard(ApplicationStatus.Draft, ApplicationStatus.Submitted);
        SubmittedAt = DateTimeOffset.UtcNow;
    }

    public void MoveToReview()
        => Guard(ApplicationStatus.Submitted, ApplicationStatus.UnderReview);

    public void MarkDocsVerified()
        => Guard(ApplicationStatus.UnderReview, ApplicationStatus.DocsVerified);

    public void RequestInfo(string reason)
    {
        if (Status != ApplicationStatus.UnderReview)
            throw new InvalidStateTransitionException(Status.ToString(), ApplicationStatus.NeedsInfo.ToString());
        Status = ApplicationStatus.NeedsInfo;
        DecisionReason = reason;
    }

    public void ReturnToReview()
        => Guard(ApplicationStatus.NeedsInfo, ApplicationStatus.UnderReview);

    public void MakeOffer(Guid decidedByUserId)
    {
        if (Status != ApplicationStatus.UnderReview && Status != ApplicationStatus.DocsVerified)
            throw new InvalidStateTransitionException(Status.ToString(), ApplicationStatus.OfferMade.ToString());
        Status = ApplicationStatus.OfferMade;
        DecidedByUserId = decidedByUserId;
        DecidedAt = DateTimeOffset.UtcNow;
    }

    public void AcceptOffer()
        => Guard(ApplicationStatus.OfferMade, ApplicationStatus.OfferAccepted);

    public void MarkFeesPaid()
        => Guard(ApplicationStatus.OfferAccepted, ApplicationStatus.FeesPaid);

    public void ClearMedicals()
        => Guard(ApplicationStatus.FeesPaid, ApplicationStatus.MedicalsCleared);

    public void Enrol()
        => Guard(ApplicationStatus.MedicalsCleared, ApplicationStatus.Enrolled);

    public void Withdraw()
    {
        var withdrawable = new[]
        {
            ApplicationStatus.Draft, ApplicationStatus.Submitted, ApplicationStatus.UnderReview,
            ApplicationStatus.NeedsInfo, ApplicationStatus.DocsVerified, ApplicationStatus.OfferMade
        };
        if (!withdrawable.Contains(Status))
            throw new InvalidStateTransitionException(Status.ToString(), ApplicationStatus.Withdrawn.ToString());
        Status = ApplicationStatus.Withdrawn;
    }

    public void Reject(Guid decidedByUserId, string reason)
    {
        if (Status != ApplicationStatus.UnderReview && Status != ApplicationStatus.DocsVerified)
            throw new InvalidStateTransitionException(Status.ToString(), ApplicationStatus.Rejected.ToString());
        Status = ApplicationStatus.Rejected;
        DecisionReason = reason;
        DecidedByUserId = decidedByUserId;
        DecidedAt = DateTimeOffset.UtcNow;
    }

    private void Guard(ApplicationStatus from, ApplicationStatus to)
    {
        if (Status != from)
            throw new InvalidStateTransitionException(Status.ToString(), to.ToString());
        Status = to;
    }
}
