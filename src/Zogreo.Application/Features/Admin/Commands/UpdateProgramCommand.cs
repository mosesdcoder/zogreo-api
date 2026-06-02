using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Admin.Commands;

public record UpdateProgramCommand(Guid Id, string? Name, ProgramLevel? Level, DeliveryMode? Mode, string? DurationLabel, string? Description, bool? Active)
    : ICommand<ProgramDto>;

public class UpdateProgramCommandHandler(IApplicationDbContext db)
    : ICommandHandler<UpdateProgramCommand, ProgramDto>
{
    public async Task<ProgramDto> Handle(UpdateProgramCommand cmd, CancellationToken ct)
    {
        var prog = await db.Programs.FirstOrDefaultAsync(p => p.Id == cmd.Id, ct)
            ?? throw AppException.NotFound("Program not found.");

        if (cmd.Name != null) prog.Name = cmd.Name;
        if (cmd.Level.HasValue) prog.Level = cmd.Level.Value;
        if (cmd.Mode.HasValue) prog.Mode = cmd.Mode.Value;
        if (cmd.DurationLabel != null) prog.DurationLabel = cmd.DurationLabel;
        if (cmd.Description != null) prog.Description = cmd.Description;
        if (cmd.Active.HasValue) prog.Active = cmd.Active.Value;

        await db.SaveChangesAsync(ct);
        return new ProgramDto(prog.Id, prog.Name, prog.Level.ToString(), prog.Mode.ToString(), prog.DurationLabel, prog.Description, prog.Active);
    }
}
