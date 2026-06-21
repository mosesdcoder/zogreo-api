using Zogreo.Domain.Common;

namespace Zogreo.Domain.Entities;

/// <summary>Tracks the Moodle account provisioned for each enrolled student.</summary>
public class MoodleUser : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid UserId { get; set; }
    public long MoodleUserId { get; set; }
    public string MoodleUsername { get; set; } = string.Empty;
    public DateTimeOffset ProvisionedAt { get; set; }

    public Student Student { get; set; } = null!;
    public User User { get; set; } = null!;
}
