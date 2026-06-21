using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Domain.Entities;

namespace Zogreo.Application.Features.Lms.Commands;

public record UpsertTimetableEntryCommand(
    Guid? Id,
    Guid ProgramId,
    Guid IntakeId,
    string SubjectName,
    string LecturerName,
    string Room,
    int DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime) : ICommand<Guid>;

public class UpsertTimetableEntryCommandHandler(
    IApplicationDbContext db,
    ITenantProvider tenant) : ICommandHandler<UpsertTimetableEntryCommand, Guid>
{
    public async Task<Guid> Handle(UpsertTimetableEntryCommand cmd, CancellationToken ct)
    {
        TimetableEntry entry;

        if (cmd.Id.HasValue)
        {
            entry = await db.TimetableEntries.FirstOrDefaultAsync(t => t.Id == cmd.Id.Value, ct)
                ?? throw new InvalidOperationException("Timetable entry not found.");
        }
        else
        {
            entry = new TimetableEntry { OrganizationId = tenant.OrganizationId };
            db.TimetableEntries.Add(entry);
        }

        entry.ProgramId    = cmd.ProgramId;
        entry.IntakeId     = cmd.IntakeId;
        entry.SubjectName  = cmd.SubjectName;
        entry.LecturerName = cmd.LecturerName;
        entry.Room         = cmd.Room;
        entry.DayOfWeek    = cmd.DayOfWeek;
        entry.StartTime    = cmd.StartTime;
        entry.EndTime      = cmd.EndTime;
        entry.Active       = true;

        await db.SaveChangesAsync(ct);
        return entry.Id;
    }
}
