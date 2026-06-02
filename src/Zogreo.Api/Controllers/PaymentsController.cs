using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Payments.Commands;
using Zogreo.Application.Features.Payments.Queries;

namespace Zogreo.Api.Controllers;

/// <summary>Invoices, payment initiation and status</summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class PaymentsController(ISender sender) : ControllerBase
{
    /// <summary>List all invoices for an application.</summary>
    [HttpGet("applications/{id:guid}/invoices")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoices(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetInvoicesQuery(id), ct));

    /// <summary>Initiate a Paystack payment for an invoice. Returns an authorization URL or STK push reference.</summary>
    [HttpPost("payments/initiate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Initiate(InitiatePaymentRequest req, CancellationToken ct)
        => Ok(await sender.Send(new InitiatePaymentCommand(req.InvoiceId, req.Channel), ct));

    /// <summary>Check payment status (re-verifies with Paystack if still pending).</summary>
    [HttpGet("payments/{reference}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Status(string reference, CancellationToken ct)
        => Ok(await sender.Send(new GetPaymentStatusQuery(reference), ct));
}

public record InitiatePaymentRequest(Guid InvoiceId, string Channel);

/// <summary>Paystack webhook receiver (machine-to-machine, no auth)</summary>
[ApiController]
[AllowAnonymous]
public class PaystackWebhookController(ISender sender, IConfiguration config) : ControllerBase
{
    /// <summary>
    /// Receives Paystack charge.success events. Verifies HMAC-SHA512 signature before processing.
    /// Idempotent — posting the same event twice credits the invoice only once.
    /// </summary>
    [HttpPost("webhooks/paystack")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Handle()
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        var secret    = config["Paystack:SecretKey"]!;
        var signature = Request.Headers["x-paystack-signature"].ToString();
        var computed  = Convert.ToHexString(
            HMACSHA512.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(rawBody))
        ).ToLowerInvariant();

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computed),
                Encoding.UTF8.GetBytes(signature)))
            return Unauthorized();

        var doc       = JsonDocument.Parse(rawBody);
        var eventType = doc.RootElement.GetProperty("event").GetString();

        if (eventType == "charge.success")
        {
            var reference = doc.RootElement.GetProperty("data").GetProperty("reference").GetString()!;
            await sender.Send(new ApplyPaymentConfirmationCommand(reference, rawBody));
        }

        return Ok();
    }
}
