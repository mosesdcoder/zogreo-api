namespace Zogreo.Application.Features.Offers.DTOs;

public record OfferDto(
    Guid Id, string Status, string? LetterUrl, string? Conditions,
    DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt, DateTimeOffset? AcceptedAt);
