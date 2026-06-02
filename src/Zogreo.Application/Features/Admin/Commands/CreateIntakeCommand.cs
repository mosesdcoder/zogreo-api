using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;
using Zogreo.Domain.Entities;

namespace Zogreo.Application.Features.Admin.Commands;

public record CreateIntakeCommand(Guid ProgramId, string Name, DateTimeOffset OpensAt, DateTimeOffset ClosesAt, DateTimeOffset StartsAt, int? Capacity)
    : ICommand<IntakeDto>;

public class CreateIntakeCommandValidator : AbstractValidator<CreateIntakeCommand>
{
    public CreateIntakeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.ClosesAt).GreaterThan(x => x.OpensAt).WithMessage("ClosesAt must be after OpensAt.");
        RuleFor(x => x.StartsAt).GreaterThan(x => x.ClosesAt).WithMessage("StartsAt must be after ClosesAt.");
    }
}

public class CreateIntakeCommandHandler(
    IApplicationDbContext db,
    ITenantProvider tenant) : ICommandHandler<CreateIntakeCommand, IntakeDto>
{
    public async Task<IntakeDto> Handle(CreateIntakeCommand cmd, CancellationToken ct)
    {
        var program = await db.Programs.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == cmd.ProgramId, ct)
            ?? throw AppException.NotFound("Program not found.");

        var intake = new Intake
        {
            OrganizationId = tenant.OrganizationId,
            ProgramId = cmd.ProgramId,
            Name = cmd.Name,
            OpensAt = cmd.OpensAt,
            ClosesAt = cmd.ClosesAt,
            StartsAt = cmd.StartsAt,
            Capacity = cmd.Capacity,
            Active = true
        };
        db.Intakes.Add(intake);
        await db.SaveChangesAsync(ct);

        return new IntakeDto(intake.Id, intake.ProgramId, program.Name, intake.Name, intake.OpensAt, intake.ClosesAt, intake.StartsAt, intake.Capacity, intake.Active);
    }
}
