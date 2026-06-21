using Hangfire;
using Zogreo.Application.Common.Interfaces;

namespace Zogreo.Infrastructure.Moodle;

/// <summary>
/// Enqueues a Hangfire background job to provision the student in Moodle.
/// The payment webhook returns immediately; Moodle API is called asynchronously.
/// </summary>
public class HangfireMoodleProvisioningTrigger : IMoodleProvisioningTrigger
{
    public Task TriggerAsync(Guid studentId, CancellationToken ct = default)
    {
        BackgroundJob.Enqueue<MoodleProvisioningService>(s => s.ProvisionStudentAsync(studentId, CancellationToken.None));
        return Task.CompletedTask;
    }
}
