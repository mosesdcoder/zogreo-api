using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Domain.Entities;
using Zogreo.Infrastructure.Persistence;

namespace Zogreo.Infrastructure.Moodle;

/// <summary>
/// Called when a student reaches Enrolled status.
/// Creates their Moodle account and enrols them in their program's course.
/// </summary>
public class MoodleProvisioningService(
    IMoodleClient moodle,
    ApplicationDbContext db,
    ILogger<MoodleProvisioningService> logger)
{
    public async Task ProvisionStudentAsync(Guid studentId, CancellationToken ct = default)
    {
        var student = await db.Students
            .Include(s => s.User)
            .Include(s => s.Application)
                .ThenInclude(a => a.Program)
            .Include(s => s.Application)
                .ThenInclude(a => a.Intake)
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);

        if (student is null)
        {
            logger.LogWarning("MoodleProvision: student {Id} not found", studentId);
            return;
        }

        // Already provisioned?
        var existing = await db.MoodleUsers.FirstOrDefaultAsync(m => m.StudentId == studentId, ct);
        if (existing is not null)
        {
            logger.LogInformation("MoodleProvision: student {Id} already provisioned (MoodleUserId={Mid})", studentId, existing.MoodleUserId);
            return;
        }

        var user    = student.User;
        var program = student.Application.Program;
        var intake  = student.Application.Intake;

        // ── 1. Find or create Moodle user ─────────────────────────────────────
        var username = $"{user.Email.Split('@')[0].ToLower().Replace(".", "_")}_{student.AdmissionNumber.ToLower()}";
        var moodleUserId = await moodle.FindUserAsync(username, ct)
            ?? await moodle.CreateUserAsync(
                username: username,
                password: GenerateTempPassword(student.AdmissionNumber),
                firstName: user.FullName.Split(' ')[0],
                lastName: string.Join(' ', user.FullName.Split(' ').Skip(1).DefaultIfEmpty(".")),
                email: user.Email,
                ct: ct);

        logger.LogInformation("MoodleProvision: Moodle user {Username} → id={Mid}", username, moodleUserId);

        // ── 2. Find or create course shell for the program ────────────────────
        var courseShortName = $"PROG-{program.Id.ToString()[..8].ToUpper()}";
        var moodleCourseId = await moodle.FindCourseAsync(courseShortName, ct)
            ?? await moodle.CreateCourseAsync(
                fullName: program.Name,
                shortName: courseShortName,
                summary: program.Description ?? $"{program.Name} — {program.DurationLabel}",
                ct: ct);

        logger.LogInformation("MoodleProvision: course '{Name}' → id={Cid}", program.Name, moodleCourseId);

        // ── 3. Enrol student in course ────────────────────────────────────────
        await moodle.EnrolUserAsync(moodleUserId, moodleCourseId, ct);

        // ── 4. Persist the mapping ────────────────────────────────────────────
        db.MoodleUsers.Add(new MoodleUser
        {
            OrganizationId  = student.OrganizationId,
            StudentId       = student.Id,
            UserId          = user.Id,
            MoodleUserId    = moodleUserId,
            MoodleUsername  = username,
            ProvisionedAt   = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);

        logger.LogInformation("MoodleProvision: student {AdmNo} fully provisioned in Moodle", student.AdmissionNumber);
    }

    private static string GenerateTempPassword(string admissionNumber)
        => $"Zogreo@{admissionNumber}!";
}
