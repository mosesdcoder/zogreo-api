using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Catalog.Queries;

namespace Zogreo.Api.Controllers;

/// <summary>Programs and intakes (public, no auth required)</summary>
[ApiController]
[AllowAnonymous]
[Produces("application/json")]
public class CatalogController(ISender sender) : ControllerBase
{
    /// <summary>List all active programs.</summary>
    [HttpGet("programs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPrograms(CancellationToken ct)
        => Ok(await sender.Send(new GetProgramsQuery(), ct));

    /// <summary>Get a single program by ID.</summary>
    [HttpGet("programs/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProgram(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetProgramQuery(id), ct));

    /// <summary>List intakes, optionally filtered by program.</summary>
    [HttpGet("intakes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIntakes([FromQuery] Guid? programId, CancellationToken ct)
        => Ok(await sender.Send(new GetIntakesQuery(programId), ct));
}
