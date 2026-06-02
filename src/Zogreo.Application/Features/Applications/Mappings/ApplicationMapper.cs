using Zogreo.Application.Features.Applications.DTOs;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Applications.Mappings;

internal static class ApplicationMapper
{
    internal static ApplicationSummaryDto ToSummary(
        Domain.Entities.Application app, string programName, string intakeName) =>
        new(app.Id, app.ProgramId, programName, app.IntakeId, intakeName,
            app.Status.ToString(), WhatNext(app.Status), app.CreatedAt);

    internal static string? WhatNext(ApplicationStatus s) => s switch
    {
        ApplicationStatus.Draft           => "Complete your application and pay the application fee, then submit.",
        ApplicationStatus.Submitted       => "Your application is submitted and awaiting review.",
        ApplicationStatus.UnderReview     => "Your application is under review.",
        ApplicationStatus.NeedsInfo       => "Additional information has been requested.",
        ApplicationStatus.DocsVerified    => "Documents verified. Awaiting admission decision.",
        ApplicationStatus.OfferMade       => "You have an offer! Pay the acceptance fee and accept.",
        ApplicationStatus.OfferAccepted   => "Offer accepted. Pay admission and technology fees.",
        ApplicationStatus.FeesPaid        => "Fees paid. Upload medical report and pay medical fee.",
        ApplicationStatus.MedicalsCleared => "Medicals cleared. Awaiting enrolment.",
        ApplicationStatus.Enrolled        => "Congratulations! You are enrolled.",
        ApplicationStatus.Rejected        => "Your application was not successful.",
        ApplicationStatus.Withdrawn       => "Application withdrawn.",
        _                                 => null
    };
}
