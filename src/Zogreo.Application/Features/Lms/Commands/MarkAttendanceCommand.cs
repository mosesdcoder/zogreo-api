using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Domain.Entities;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Lms.Commands;

public record MarkAttendanceItem(Guid StudentId, AttendanceStatus Status, string? Note);
public record MarkAttendanceCommand(Guid TimetableEntryId, DateOnly Date, List<MarkAttendanceItem> Records) : ICommand<int>;

public class MarkAttendanceCommandHandler(
    IApplicationDbContext db,
    ITenantProvider tenant) : ICommandHandler<MarkAttendanceCommand, int>
{
    public async Task<int> Handle(MarkAttendanceCommand cmd, CancellationToken ct)
    {
        var entryExists = await db.TimetableEntries
            .AnyAsync(t => t.Id == cmd.TimetableEntryId, ct);
        if (!entryExists) throw AppException.NotFound("Timetable entry not found.");

        var count = 0;
        foreach (var item in cmd.Records)
        {
            var existing = await db.AttendanceRecords
                .FirstOrDefaultAsync(a =>
                    a.TimetableEntryId == cmd.TimetableEntryId &&
                    a.StudentId == item.StudentId &&
                    a.Date == cmd.Date, ct);

            if (existing is not null)
            {
                existing.Status = item.Status;
                existing.Note   = item.Note;
            }
            else
            {
                db.AttendanceRecords.Add(new AttendanceRecord
                {
                    OrganizationId    = tenant.OrganizationId,
                    StudentId         = item.StudentId,
                    TimetableEntryId  = cmd.TimetableEntryId,
                    Date              = cmd.Date,
                    Status            = item.Status,
                    Note              = item.Note,
                    MarkedByUserId    = tenant.UserId,
                });
            }
            count++;
        }

        await db.SaveChangesAsync(ct);
        return count;
    }
}
