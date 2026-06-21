using Zogreo.Domain.Common;

namespace Zogreo.Domain.Entities;

public class TimetableEntry : TenantEntity
{
    public Guid ProgramId { get; set; }
    public Guid IntakeId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string LecturerName { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    /// <summary>0=Sunday … 6=Saturday</summary>
    public int DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool Active { get; set; } = true;

    public Program Program { get; set; } = null!;
    public Intake Intake { get; set; } = null!;
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
