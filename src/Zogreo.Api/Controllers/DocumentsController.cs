using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Documents.Commands;
using Zogreo.Application.Features.Documents.Queries;
using Zogreo.Domain.Enums;

namespace Zogreo.Api.Controllers;

/// <summary>Document upload and checklist for applications</summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class DocumentsController(ISender sender) : ControllerBase
{
    /// <summary>Upload a document (JPEG/PNG/PDF, max 10 MB) for an application.</summary>
    [HttpPost("applications/{id:guid}/documents")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Upload(Guid id, [FromForm] DocumentType type, IFormFile file, CancellationToken ct)
        => Ok(await sender.Send(new UploadDocumentCommand(id, type, new FormFileProxy(file)), ct));

    /// <summary>List all documents uploaded for an application.</summary>
    [HttpGet("applications/{id:guid}/documents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetDocumentsQuery(id), ct));
}

internal class FormFileProxy(IFormFile file) : IFileProxy
{
    public string FileName    => file.FileName;
    public string ContentType => file.ContentType;
    public long   Length      => file.Length;
    public async Task CopyToAsync(Stream target, CancellationToken ct = default)
        => await file.CopyToAsync(target, ct);
}
