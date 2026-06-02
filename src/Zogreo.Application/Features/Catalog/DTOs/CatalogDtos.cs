namespace Zogreo.Application.Features.Catalog.DTOs;

public record ProgramDto(Guid Id, string Name, string Level, string Mode, string DurationLabel, string? Description);
public record IntakeDto(Guid Id, Guid ProgramId, string Name, DateTimeOffset OpensAt, DateTimeOffset ClosesAt, DateTimeOffset StartsAt, int? Capacity);
