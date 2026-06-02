using Zogreo.Application.Features.Documents.DTOs;
using Zogreo.Application.Features.Payments.DTOs;

namespace Zogreo.Application.Features.Admin.DTOs;

public record AdminAppListItem(Guid Id, string ApplicantName, string ApplicantEmail, string ProgramName, string IntakeName, string Status, DateTimeOffset? SubmittedAt, DateTimeOffset CreatedAt);
public record AdminAppDetail(Guid Id, Guid UserId, string ApplicantName, string Email, string Phone, string ProgramName, string IntakeName, string Status, string? PersonalJson, string? EducationHistoryJson, string? NextOfKinJson, string? HowDidYouHear, DateTimeOffset? SubmittedAt, string? DecisionReason, IEnumerable<DocumentDto> Documents, IEnumerable<InvoiceDto> Invoices);
public record AdminPaymentItem(Guid Id, string Reference, string FeeCode, string Status, decimal AmountGross, decimal ProviderFee, decimal TechnologyFee, decimal AmountNetToSchool, string Channel, DateTimeOffset? CompletedAt);
public record AdminStudentItem(Guid Id, string AdmissionNumber, string FullName, string Email, string ProgramName, string Status, DateTimeOffset EnrolledAt);
public record AdminUserDto(Guid Id, string FullName, string Email, string Phone, bool PhoneVerified, string Role, bool Active, DateTimeOffset CreatedAt);
public record FeeTypeDto(Guid Id, string Code, string Name, decimal Amount, bool Refundable, bool Active);
public record ProgramDto(Guid Id, string Name, string Level, string Mode, string DurationLabel, string? Description, bool Active);
public record IntakeDto(Guid Id, Guid ProgramId, string ProgramName, string Name, DateTimeOffset OpensAt, DateTimeOffset ClosesAt, DateTimeOffset StartsAt, int? Capacity, bool Active);
public record PagedResult<T>(int Total, int Page, IEnumerable<T> Items);
