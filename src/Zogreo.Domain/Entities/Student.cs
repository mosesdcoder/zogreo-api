using Zogreo.Domain.Common;
using Zogreo.Domain.Enums;

namespace Zogreo.Domain.Entities;

public class Student : TenantEntity
{
    public Guid ApplicationId { get; set; }
    public Guid UserId { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public StudentStatus Status { get; set; } = StudentStatus.Active;
    public DateTimeOffset EnrolledAt { get; set; }

    public Application Application { get; set; } = null!;
    public User User { get; set; } = null!;
}
