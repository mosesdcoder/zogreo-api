namespace Zogreo.Application.Features.Documents.DTOs;

public record DocumentDto(
    Guid Id, string Type, string FileUrl, string OriginalFileName,
    string Status, string? ReviewReason, DateTimeOffset CreatedAt);
