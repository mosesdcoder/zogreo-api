using Zogreo.Domain.Common;
using Zogreo.Domain.Enums;

namespace Zogreo.Domain.Entities;

public class AttendanceRecord : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid TimetableEntryId { get; set; }
    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Absent;
    public string? Note { get; set; }
    public Guid MarkedByUserId { get; set; }

    public Student Student { get; set; } = null!;
    public TimetableEntry TimetableEntry { get; set; } = null!;
    public User MarkedBy { get; set; } = null!;
}
