namespace Zogreo.Application.Common.Interfaces;

/// <summary>
/// Abstracts the act of triggering Moodle provisioning for a newly enrolled student.
/// The Infrastructure layer implements this via a Hangfire background job so the
/// webhook/payment handler returns immediately without waiting for Moodle's API.
/// </summary>
public interface IMoodleProvisioningTrigger
{
    Task TriggerAsync(Guid studentId, CancellationToken ct = default);
}
