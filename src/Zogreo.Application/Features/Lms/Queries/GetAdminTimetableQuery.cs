using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;

namespace Zogreo.Application.Features.Lms.Queries;

public record GetAdminTimetableQuery(Guid? IntakeId) : IQuery<List<TimetableEntryDto>>;

public class GetAdminTimetableQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetAdminTimetableQuery, List<TimetableEntryDto>>
{
    public async Task<List<TimetableEntryDto>> Handle(GetAdminTimetableQuery query, CancellationToken ct)
    {
        var q = db.TimetableEntries
            .Include(t => t.Program)
            .Include(t => t.Intake)
            .Where(t => t.Active);

        if (query.IntakeId.HasValue)
            q = q.Where(t => t.IntakeId == query.IntakeId.Value);

        return await q
            .OrderBy(t => t.IntakeId).ThenBy(t => t.DayOfWeek).ThenBy(t => t.StartTime)
            .Select(t => new TimetableEntryDto(
                t.Id, t.SubjectName, t.LecturerName, t.Room,
                t.DayOfWeek,
                t.DayOfWeek == 0 ? "Sunday" : t.DayOfWeek == 1 ? "Monday" :
                t.DayOfWeek == 2 ? "Tuesday" : t.DayOfWeek == 3 ? "Wednesday" :
                t.DayOfWeek == 4 ? "Thursday" : t.DayOfWeek == 5 ? "Friday" : "Saturday",
                t.StartTime.ToString(),
                t.EndTime.ToString(),
                t.ProgramId, t.Program.Name,
                t.IntakeId, t.Intake.Name))
            .ToListAsync(ct);
    }
}
