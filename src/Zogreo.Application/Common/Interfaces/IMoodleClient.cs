namespace Zogreo.Application.Common.Interfaces;

public interface IMoodleClient
{
    /// <summary>Create a Moodle user account. Returns the Moodle user ID.</summary>
    Task<long> CreateUserAsync(string username, string password, string firstName, string lastName, string email, CancellationToken ct = default);

    /// <summary>Enrol a Moodle user into a course as a student.</summary>
    Task EnrolUserAsync(long moodleUserId, long moodleCourseId, CancellationToken ct = default);

    /// <summary>Create a course shell in Moodle. Returns the Moodle course ID.</summary>
    Task<long> CreateCourseAsync(string fullName, string shortName, string summary, CancellationToken ct = default);

    /// <summary>Generate a Moodle auto-login URL for the given username (SSO).</summary>
    Task<string> GetAutoLoginUrlAsync(string username, string returnUrl = "/my", CancellationToken ct = default);

    /// <summary>Look up an existing Moodle user by username. Returns null if not found.</summary>
    Task<long?> FindUserAsync(string username, CancellationToken ct = default);

    /// <summary>Look up a Moodle course by short name. Returns null if not found.</summary>
    Task<long?> FindCourseAsync(string shortName, CancellationToken ct = default);
}
