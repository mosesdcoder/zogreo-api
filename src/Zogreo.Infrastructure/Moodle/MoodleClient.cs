using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zogreo.Application.Common.Interfaces;

namespace Zogreo.Infrastructure.Moodle;

/// <summary>
/// Calls the Moodle REST API (token-based web services).
/// Base URL  : Moodle:BaseUrl  (e.g. https://lms.zogreo.online)
/// Admin token: Moodle:Token   (generated in Moodle → Site admin → Web services → Manage tokens)
/// </summary>
public class MoodleClient(
    HttpClient http,
    IConfiguration config,
    ILogger<MoodleClient> logger) : IMoodleClient
{
    private string Token => config["Moodle:Token"] ?? throw new InvalidOperationException("Moodle:Token not configured.");

    // ── Core REST call ────────────────────────────────────────────────────────

    private async Task<JsonElement> CallAsync(string function, Dictionary<string, string> parameters, CancellationToken ct)
    {
        var form = new Dictionary<string, string>(parameters)
        {
            ["wstoken"]         = Token,
            ["wsfunction"]      = function,
            ["moodlewsrestformat"] = "json"
        };

        var response = await http.PostAsync("/webservice/rest/server.php",
            new FormUrlEncodedContent(form), ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        logger.LogDebug("Moodle [{Function}] → {Body}", function, body);

        var doc = JsonDocument.Parse(body).RootElement;

        // Moodle returns {"exception":...} on error
        if (doc.TryGetProperty("exception", out var ex))
            throw new InvalidOperationException($"Moodle error [{function}]: {ex.GetString()} — {doc.GetProperty("message").GetString()}");

        return doc;
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    public async Task<long> CreateUserAsync(string username, string password, string firstName, string lastName, string email, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string>
        {
            ["users[0][username]"]  = username,
            ["users[0][password]"]  = password,
            ["users[0][firstname]"] = firstName,
            ["users[0][lastname]"]  = lastName,
            ["users[0][email]"]     = email,
            ["users[0][auth]"]      = "manual",
        };
        var result = await CallAsync("core_user_create_users", p, ct);
        return result[0].GetProperty("id").GetInt64();
    }

    public async Task<long?> FindUserAsync(string username, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string>
        {
            ["criteria[0][key]"]   = "username",
            ["criteria[0][value]"] = username,
        };
        var result = await CallAsync("core_user_get_users", p, ct);
        var users = result.GetProperty("users");
        if (users.GetArrayLength() == 0) return null;
        return users[0].GetProperty("id").GetInt64();
    }

    // ── Courses ───────────────────────────────────────────────────────────────

    public async Task<long> CreateCourseAsync(string fullName, string shortName, string summary, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string>
        {
            ["courses[0][fullname]"]  = fullName,
            ["courses[0][shortname]"] = shortName,
            ["courses[0][summary]"]   = summary,
            ["courses[0][categoryid]"] = "1",   // Default category
            ["courses[0][visible]"]   = "0",    // Hidden until lecturer publishes
        };
        var result = await CallAsync("core_course_create_courses", p, ct);
        return result[0].GetProperty("id").GetInt64();
    }

    public async Task<long?> FindCourseAsync(string shortName, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string>
        {
            ["field"] = "shortname",
            ["value"] = shortName,
        };
        try
        {
            var result = await CallAsync("core_course_get_courses_by_field", p, ct);
            var courses = result.GetProperty("courses");
            if (courses.GetArrayLength() == 0) return null;
            return courses[0].GetProperty("id").GetInt64();
        }
        catch { return null; }
    }

    // ── Enrolment ─────────────────────────────────────────────────────────────

    public async Task EnrolUserAsync(long moodleUserId, long moodleCourseId, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string>
        {
            ["enrolments[0][roleid]"]   = "5",   // 5 = student role in Moodle
            ["enrolments[0][userid]"]   = moodleUserId.ToString(),
            ["enrolments[0][courseid]"] = moodleCourseId.ToString(),
        };
        await CallAsync("enrol_manual_enrol_users", p, ct);
    }

    // ── SSO auto-login ────────────────────────────────────────────────────────

    public async Task<string> GetAutoLoginUrlAsync(string username, string returnUrl = "/my", CancellationToken ct = default)
    {
        var p = new Dictionary<string, string>
        {
            ["username"]  = username,
            ["wantsurl"]  = returnUrl,
        };
        var result = await CallAsync("auth_userkey_request_login_url", p, ct);

        // Returns { "loginurl": "https://lms.zogreo.online/auth/userkey/login.php?key=..." }
        return result.GetProperty("loginurl").GetString()
            ?? throw new InvalidOperationException("Moodle did not return a login URL.");
    }
}
