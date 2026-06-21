using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Lms.Commands;
using Zogreo.Application.Features.Lms.Queries;

namespace Zogreo.Api.Controllers;

[ApiController]
[Route("lms")]
[ApiExplorerSettings(GroupName = "LMS")]
public class LmsController(ISender sender) : ControllerBase
{
    /// <summary>Get Moodle SSO auto-login URL for the current student.</summary>
    [HttpGet("sso-url")]
    [Authorize]
    public async Task<IActionResult> GetSsoUrl([FromQuery] string returnUrl = "/my", CancellationToken ct = default)
    {
        var url = await sender.Send(new GetSsoUrlQuery(returnUrl), ct);
        return Ok(url);
    }

    /// <summary>Get the current student's weekly timetable.</summary>
    [HttpGet("timetable/me")]
    [Authorize]
    public async Task<IActionResult> GetMyTimetable(CancellationToken ct = default)
    {
        var result = await sender.Send(new GetMyTimetableQuery(), ct);
        return Ok(result);
    }

    /// <summary>Get the current student's attendance summary.</summary>
    [HttpGet("attendance/me")]
    [Authorize]
    public async Task<IActionResult> GetMyAttendance(CancellationToken ct = default)
    {
        var result = await sender.Send(new GetMyAttendanceQuery(), ct);
        return Ok(result);
    }

    /// <summary>Admin: create or update a timetable entry.</summary>
    [HttpPost("timetable")]
    [Authorize(Roles = "Registrar,SuperAdmin")]
    public async Task<IActionResult> UpsertTimetableEntry(
        [FromBody] UpsertTimetableEntryCommand cmd, CancellationToken ct = default)
    {
        var id = await sender.Send(cmd, ct);
        return Ok(id);
    }

    /// <summary>Admin/Lecturer: mark attendance for a class session.</summary>
    [HttpPost("attendance")]
    [Authorize(Roles = "Registrar,SuperAdmin")]
    public async Task<IActionResult> MarkAttendance(
        [FromBody] MarkAttendanceCommand cmd, CancellationToken ct = default)
    {
        var count = await sender.Send(cmd, ct);
        return Ok(new { marked = count });
    }

    /// <summary>Admin: get all timetable entries (optionally filtered by intake).</summary>
    [HttpGet("timetable")]
    [Authorize(Roles = "Registrar,Bursar,SuperAdmin")]
    public async Task<IActionResult> GetTimetable([FromQuery] Guid? intakeId, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAdminTimetableQuery(intakeId), ct);
        return Ok(result);
    }
}
