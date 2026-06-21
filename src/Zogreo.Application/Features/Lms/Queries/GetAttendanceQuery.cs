using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Lms.Queries;

public record AttendanceRecordDto(
    Guid Id,
    string SubjectName,
    string Date,
    AttendanceStatus Status,
    string? Note);

public record AttendanceSummaryDto(
    string SubjectName,
    int TotalClasses,
    int Present,
    int Late,
    int Absent,
    decimal AttendancePercent,
    List<AttendanceRecordDto> Records);

public record GetMyAttendanceQuery : IQuery<List<AttendanceSummaryDto>>;

public class GetMyAttendanceQueryHandler(
    IApplicationDbContext db,
    ITenantProvider tenant) : IQueryHandler<GetMyAttendanceQuery, List<AttendanceSummaryDto>>
{
    public async Task<List<AttendanceSummaryDto>> Handle(GetMyAttendanceQuery query, CancellationToken ct)
    {
        var student = await db.Students
            .FirstOrDefaultAsync(s => s.UserId == tenant.UserId, ct);

        if (student is null) return [];

        var records = await db.AttendanceRecords
            .Include(a => a.TimetableEntry)
            .Where(a => a.StudentId == student.Id)
            .OrderByDescending(a => a.Date)
            .ToListAsync(ct);

        return records
            .GroupBy(a => a.TimetableEntry.SubjectName)
            .Select(g =>
            {
                var total   = g.Count();
                var present = g.Count(a => a.Status == AttendanceStatus.Present);
                var late    = g.Count(a => a.Status == AttendanceStatus.Late);
                var absent  = g.Count(a => a.Status == AttendanceStatus.Absent);
                return new AttendanceSummaryDto(
                    SubjectName: g.Key,
                    TotalClasses: total,
                    Present: present,
                    Late: late,
                    Absent: absent,
                    AttendancePercent: total == 0 ? 0 : Math.Round((present + late) * 100m / total, 1),
                    Records: g.Select(a => new AttendanceRecordDto(
                        a.Id, a.TimetableEntry.SubjectName,
                        a.Date.ToString("yyyy-MM-dd"),
                        a.Status, a.Note)).ToList());
            })
            .ToList();
    }
}
