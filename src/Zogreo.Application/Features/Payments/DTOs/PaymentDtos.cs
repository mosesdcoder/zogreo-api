namespace Zogreo.Application.Features.Payments.DTOs;

public record PaymentInitDto(Guid PaymentId, string Reference, string? AuthorizationUrl, string Status);
public record PaymentStatusDto(Guid Id, string Reference, string Status, decimal AmountGross, decimal ProviderFee, decimal TechnologyFee, decimal AmountNetToSchool, DateTimeOffset? CompletedAt);
public record InvoiceDto(Guid Id, string FeeCode, decimal Amount, decimal AmountPaid, string Status, DateTimeOffset? DueAt);
