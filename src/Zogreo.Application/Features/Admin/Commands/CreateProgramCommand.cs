using FluentValidation;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Admin.Commands;

public record CreateProgramCommand(string Name, ProgramLevel Level, DeliveryMode Mode, string DurationLabel, string? Description)
    : ICommand<ProgramDto>;

public class CreateProgramCommandValidator : AbstractValidator<CreateProgramCommand>
{
    public CreateProgramCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.DurationLabel).NotEmpty();
    }
}

public class CreateProgramCommandHandler(
    IApplicationDbContext db,
    ITenantProvider tenant) : ICommandHandler<CreateProgramCommand, ProgramDto>
{
    public async Task<ProgramDto> Handle(CreateProgramCommand cmd, CancellationToken ct)
    {
        var prog = new Domain.Entities.Program
        {
            OrganizationId = tenant.OrganizationId,
            Name = cmd.Name,
            Level = cmd.Level,
            Mode = cmd.Mode,
            DurationLabel = cmd.DurationLabel,
            Description = cmd.Description,
            Active = true
        };
        db.Programs.Add(prog);
        await db.SaveChangesAsync(ct);
        return new ProgramDto(prog.Id, prog.Name, prog.Level.ToString(), prog.Mode.ToString(), prog.DurationLabel, prog.Description, prog.Active);
    }
}
