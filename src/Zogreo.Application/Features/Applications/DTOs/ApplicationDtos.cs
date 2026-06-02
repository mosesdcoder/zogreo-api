namespace Zogreo.Application.Features.Applications.DTOs;

public record ApplicationSummaryDto(
    Guid Id, Guid ProgramId, string ProgramName, Guid IntakeId, string IntakeName,
    string Status, string? WhatNext, DateTimeOffset CreatedAt);

public record ApplicationDetailDto(
    Guid Id, Guid ProgramId, string ProgramName, Guid IntakeId, string IntakeName,
    string Status, string? PersonalJson, string? EducationHistoryJson,
    string? NextOfKinJson, string? HowDidYouHear, DateTimeOffset? SubmittedAt,
    string? DecisionReason, string? WhatNext, IEnumerable<string> StatusLadder,
    DateTimeOffset CreatedAt);

public record UnpaidInvoiceInfo(Guid InvoiceId, string FeeCode, decimal Amount, string Message);
