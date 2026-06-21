using Microsoft.Extensions.Logging;
using Zogreo.Application.Common.Interfaces;

namespace Zogreo.Infrastructure.Moodle;

/// <summary>No-op Moodle client used when Moodle:BaseUrl is not configured (e.g. local dev before Moodle is set up).</summary>
public class NullMoodleClient(ILogger<NullMoodleClient> logger) : IMoodleClient
{
    public Task<long> CreateUserAsync(string username, string password, string firstName, string lastName, string email, CancellationToken ct = default)
    { logger.LogWarning("NullMoodleClient: Moodle not configured — skipping CreateUser for {Username}", username); return Task.FromResult(0L); }

    public Task EnrolUserAsync(long moodleUserId, long moodleCourseId, CancellationToken ct = default)
    { logger.LogWarning("NullMoodleClient: Moodle not configured — skipping EnrolUser"); return Task.CompletedTask; }

    public Task<long> CreateCourseAsync(string fullName, string shortName, string summary, CancellationToken ct = default)
    { logger.LogWarning("NullMoodleClient: Moodle not configured — skipping CreateCourse for {Name}", fullName); return Task.FromResult(0L); }

    public Task<string> GetAutoLoginUrlAsync(string username, string returnUrl = "/my", CancellationToken ct = default)
        => Task.FromResult("https://lms.zogreo.online");

    public Task<long?> FindUserAsync(string username, CancellationToken ct = default)
        => Task.FromResult<long?>(null);

    public Task<long?> FindCourseAsync(string shortName, CancellationToken ct = default)
        => Task.FromResult<long?>(null);
}
