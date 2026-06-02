using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Applications.Commands;
using Zogreo.Application.Features.Applications.Queries;
using Zogreo.Application.Features.Offers.Commands;
using Zogreo.Application.Features.Offers.Queries;

namespace Zogreo.Api.Controllers;

/// <summary>Application lifecycle for applicants</summary>
[ApiController]
[Route("applications")]
[Authorize]
[Produces("application/json")]
public class ApplicationsController(ISender sender) : ControllerBase
{
    /// <summary>Create a new Draft application for a program intake.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateDraftRequest req, CancellationToken ct)
        => Ok(await sender.Send(new CreateDraftCommand(req.ProgramId, req.IntakeId), ct));

    /// <summary>Save one or more application steps (personal info, education history, next-of-kin).</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, UpdateApplicationRequest req, CancellationToken ct)
        => Ok(await sender.Send(
            new SaveApplicationStepCommand(id, req.PersonalJson, req.EducationHistoryJson, req.NextOfKinJson, req.HowDidYouHear), ct));

    /// <summary>
    /// Submit the application. Requires the application fee to be paid.
    /// Returns invoice details if the fee is unpaid.
    /// </summary>
    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new SubmitApplicationCommand(id), ct));

    /// <summary>List all applications for the current user.</summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
        => Ok(await sender.Send(new GetMyApplicationsQuery(), ct));

    /// <summary>Get a single application by ID (owner or admin).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetApplicationByIdQuery(id, AdminAllowed: true), ct));

    /// <summary>View the admission offer for an application.</summary>
    [HttpGet("{id:guid}/offer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOffer(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetOfferQuery(id), ct));

    /// <summary>Accept the admission offer. The acceptance fee must be paid first.</summary>
    [HttpPost("{id:guid}/offer/accept")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AcceptOffer(Guid id, CancellationToken ct)
    {
        await sender.Send(new AcceptOfferCommand(id), ct);
        return Ok(new { message = "Offer accepted." });
    }

    /// <summary>Withdraw the application (only possible before OfferAccepted).</summary>
    [HttpPost("{id:guid}/withdraw")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Withdraw(Guid id, CancellationToken ct)
    {
        await sender.Send(new WithdrawApplicationCommand(id), ct);
        return Ok(new { message = "Application withdrawn." });
    }
}

public record CreateDraftRequest(Guid ProgramId, Guid IntakeId);
public record UpdateApplicationRequest(string? PersonalJson, string? EducationHistoryJson, string? NextOfKinJson, string? HowDidYouHear);
