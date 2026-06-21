using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;

namespace Zogreo.Application.Features.Lms.Queries;

public record TimetableEntryDto(
    Guid Id,
    string SubjectName,
    string LecturerName,
    string Room,
    int DayOfWeek,
    string DayName,
    string StartTime,
    string EndTime,
    Guid ProgramId,
    string ProgramName,
    Guid IntakeId,
    string IntakeName);

public record GetMyTimetableQuery : IQuery<List<TimetableEntryDto>>;

public class GetMyTimetableQueryHandler(
    IApplicationDbContext db,
    ITenantProvider tenant) : IQueryHandler<GetMyTimetableQuery, List<TimetableEntryDto>>
{
    public async Task<List<TimetableEntryDto>> Handle(GetMyTimetableQuery query, CancellationToken ct)
    {
        // Get the student's intake
        var student = await db.Students
            .Include(s => s.Application)
            .FirstOrDefaultAsync(s => s.UserId == tenant.UserId, ct);

        if (student is null) return [];

        var intakeId = student.Application.IntakeId;

        return await db.TimetableEntries
            .Include(t => t.Program)
            .Include(t => t.Intake)
            .Where(t => t.IntakeId == intakeId && t.Active)
            .OrderBy(t => t.DayOfWeek).ThenBy(t => t.StartTime)
            .Select(t => new TimetableEntryDto(
                t.Id,
                t.SubjectName,
                t.LecturerName,
                t.Room,
                t.DayOfWeek,
                GetDayName(t.DayOfWeek),
                t.StartTime.ToString("HH:mm"),
                t.EndTime.ToString("HH:mm"),
                t.ProgramId,
                t.Program.Name,
                t.IntakeId,
                t.Intake.Name))
            .ToListAsync(ct);
    }

    private static string GetDayName(int day) => day switch
    {
        0 => "Sunday", 1 => "Monday", 2 => "Tuesday", 3 => "Wednesday",
        4 => "Thursday", 5 => "Friday", 6 => "Saturday", _ => "Unknown"
    };
}
