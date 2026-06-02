using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.Commands;
using Zogreo.Application.Features.Admin.Commands.Users;
using Zogreo.Application.Features.Admin.Queries;
using Zogreo.Application.Features.Admin.Queries.Intakes;
using Zogreo.Application.Features.Admin.Queries.Programs;
using Zogreo.Application.Features.Admin.Queries.Users;
using Zogreo.Domain.Enums;

namespace Zogreo.Api.Controllers;

/// <summary>Admin operations — requires Registrar, Bursar, or SuperAdmin role</summary>
[ApiController]
[Route("admin")]
[Authorize(Roles = "Registrar,Bursar,SuperAdmin")]
[Produces("application/json")]
public class AdminController(ISender sender) : ControllerBase
{
    // ── Applications ──────────────────────────────────────────────────────────

    /// <summary>Paginated application work queue, filterable by status/program/intake.</summary>
    [HttpGet("applications")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Queue(
        [FromQuery] string? status, [FromQuery] Guid? programId,
        [FromQuery] Guid? intakeId, [FromQuery] int page = 1, CancellationToken ct = default)
        => Ok(await sender.Send(new GetApplicationsQueueQuery(status, programId, intakeId, page), ct));

    [HttpGet("applications/{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetApplicationDetailQuery(id), ct));

    [HttpPatch("documents/{id:guid}")]
    public async Task<IActionResult> ReviewDocument(Guid id, ReviewDocumentRequest req, CancellationToken ct)
    {
        await sender.Send(new ReviewDocumentCommand(id, req.Status, req.Reason), ct);
        return Ok(new { message = "Document reviewed." });
    }

    [HttpPost("applications/{id:guid}/request-info")]
    public async Task<IActionResult> RequestInfo(Guid id, RequestInfoRequest req, CancellationToken ct)
    {
        await sender.Send(new RequestInfoCommand(id, req.Reason), ct);
        return Ok(new { message = "Info requested." });
    }

    [HttpPost("applications/{id:guid}/offer")]
    public async Task<IActionResult> MakeOffer(Guid id, MakeOfferRequest req, CancellationToken ct)
    {
        await sender.Send(new MakeOfferCommand(id, req.Conditions, req.ExpiryDays), ct);
        return Ok(new { message = "Offer made." });
    }

    [HttpPost("applications/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, RejectRequest req, CancellationToken ct)
    {
        await sender.Send(new RejectApplicationCommand(id, req.Reason), ct);
        return Ok(new { message = "Application rejected." });
    }

    // ── Programs ──────────────────────────────────────────────────────────────
    [HttpGet("programs")]
    public async Task<IActionResult> GetPrograms([FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await sender.Send(new GetAdminProgramsQuery(includeInactive), ct));

    [HttpGet("programs/{id:guid}")]
    public async Task<IActionResult> GetProgram(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetAdminProgramQuery(id), ct));

    [HttpPost("programs")]
    public async Task<IActionResult> CreateProgram(CreateProgramRequest req, CancellationToken ct)
        => Ok(await sender.Send(new CreateProgramCommand(req.Name, req.Level, req.Mode, req.DurationLabel, req.Description), ct));

    [HttpPatch("programs/{id:guid}")]
    public async Task<IActionResult> UpdateProgram(Guid id, UpdateProgramRequest req, CancellationToken ct)
        => Ok(await sender.Send(new UpdateProgramCommand(id, req.Name, req.Level, req.Mode, req.DurationLabel, req.Description, req.Active), ct));

    // ── Intakes ───────────────────────────────────────────────────────────────
    [HttpGet("intakes")]
    public async Task<IActionResult> GetIntakes([FromQuery] Guid? programId, [FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await sender.Send(new GetAdminIntakesQuery(programId, includeInactive), ct));

    [HttpGet("intakes/{id:guid}")]
    public async Task<IActionResult> GetIntake(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetAdminIntakeQuery(id), ct));

    [HttpPost("intakes")]
    public async Task<IActionResult> CreateIntake(CreateIntakeRequest req, CancellationToken ct)
        => Ok(await sender.Send(new CreateIntakeCommand(req.ProgramId, req.Name, req.OpensAt, req.ClosesAt, req.StartsAt, req.Capacity), ct));

    [HttpPatch("intakes/{id:guid}")]
    public async Task<IActionResult> UpdateIntake(Guid id, UpdateIntakeRequest req, CancellationToken ct)
        => Ok(await sender.Send(new UpdateIntakeCommand(id, req.Name, req.OpensAt, req.ClosesAt, req.StartsAt, req.Capacity, req.Active), ct));

    // ── Fee Types ─────────────────────────────────────────────────────────────
    [HttpGet("fee-types")]
    public async Task<IActionResult> FeeTypes(CancellationToken ct)
        => Ok(await sender.Send(new GetFeeTypesQuery(), ct));

    [HttpPost("fee-types")]
    public async Task<IActionResult> CreateFeeType(CreateFeeTypeRequest req, CancellationToken ct)
        => Ok(await sender.Send(new CreateFeeTypeCommand(req.Code, req.Name, req.Amount, req.Refundable), ct));

    [HttpPatch("fee-types/{id:guid}")]
    public async Task<IActionResult> UpdateFeeType(Guid id, UpdateFeeTypeRequest req, CancellationToken ct)
        => Ok(await sender.Send(new UpdateFeeTypeCommand(id, req.Amount, req.Active), ct));

    // ── Payments ──────────────────────────────────────────────────────────────
    [HttpGet("payments")]
    public async Task<IActionResult> Payments(
        [FromQuery] string? status, [FromQuery] int page = 1, CancellationToken ct = default)
        => Ok(await sender.Send(new GetReconciliationQuery(status, page), ct));

    [HttpGet("payments/{id:guid}")]
    public async Task<IActionResult> GetPayment(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetPaymentDetailQuery(id), ct));

    // ── Students ──────────────────────────────────────────────────────────────
    [HttpGet("students")]
    public async Task<IActionResult> Students([FromQuery] int page = 1, CancellationToken ct = default)
        => Ok(await sender.Send(new GetStudentsQuery(page), ct));

    [HttpGet("students/{id:guid}")]
    public async Task<IActionResult> GetStudent(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetStudentQuery(id), ct));

    [HttpPatch("students/{id:guid}/status")]
    public async Task<IActionResult> UpdateStudentStatus(Guid id, UpdateStudentStatusRequest req, CancellationToken ct)
        => Ok(await sender.Send(new UpdateStudentStatusCommand(id, req.Status), ct));

    // ── Users ─────────────────────────────────────────────────────────────────
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? role, [FromQuery] bool? active,
        [FromQuery] int page = 1, CancellationToken ct = default)
        => Ok(await sender.Send(new GetUsersQuery(role, active, page), ct));

    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetUserQuery(id), ct));

    [HttpPatch("users/{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateUserRequest req, CancellationToken ct)
        => Ok(await sender.Send(new UpdateUserCommand(id, req.Role, req.Active), ct));

    // ── Audit ─────────────────────────────────────────────────────────────────
    [HttpGet("audit")]
    public async Task<IActionResult> Audit([FromQuery] int page = 1, CancellationToken ct = default)
        => Ok(await sender.Send(new GetAuditQuery(page), ct));
}

// ── Request DTOs ──────────────────────────────────────────────────────────────
public record ReviewDocumentRequest(string Status, string? Reason);
public record RequestInfoRequest(string Reason);
public record MakeOfferRequest(string? Conditions, int ExpiryDays);
public record RejectRequest(string Reason);
public record UpdateFeeTypeRequest(decimal Amount, bool? Active);
public record CreateFeeTypeRequest(FeeCode Code, string Name, decimal Amount, bool Refundable);
public record CreateProgramRequest(string Name, ProgramLevel Level, DeliveryMode Mode, string DurationLabel, string? Description);
public record UpdateProgramRequest(string? Name, ProgramLevel? Level, DeliveryMode? Mode, string? DurationLabel, string? Description, bool? Active);
public record CreateIntakeRequest(Guid ProgramId, string Name, DateTimeOffset OpensAt, DateTimeOffset ClosesAt, DateTimeOffset StartsAt, int? Capacity);
public record UpdateIntakeRequest(string? Name, DateTimeOffset? OpensAt, DateTimeOffset? ClosesAt, DateTimeOffset? StartsAt, int? Capacity, bool? Active);
public record UpdateStudentStatusRequest(StudentStatus Status);
public record UpdateUserRequest(Role? Role, bool? Active);
