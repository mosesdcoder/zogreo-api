using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Payments.DTOs;
using Zogreo.Domain.Entities;
using Zogreo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Zogreo.Application.Features.Payments.Commands;

public record InitiatePaymentCommand(Guid InvoiceId, string Channel) : ICommand<PaymentInitDto>;

public class InitiatePaymentCommandHandler(
    IApplicationDbContext db,
    ITenantProvider tenant,
    IPaystackClient paystack,
    IPaymentSettings settings) : ICommandHandler<InitiatePaymentCommand, PaymentInitDto>
{
    public async Task<PaymentInitDto> Handle(InitiatePaymentCommand cmd, CancellationToken ct)
    {
        var userId = tenant.UserId ?? throw AppException.Unauthorized();

        var invoice = await db.Invoices
            .Include(i => i.Application).ThenInclude(a => a.User)
            .FirstOrDefaultAsync(i => i.Id == cmd.InvoiceId, ct)
            ?? throw AppException.NotFound("Invoice not found.");

        if (invoice.Application.UserId != userId && tenant.UserRole == Role.Applicant.ToString())
            throw AppException.Forbidden();
        if (invoice.Status == InvoiceStatus.Paid)
            throw AppException.Conflict("Invoice is already paid.");

        var nonce = Guid.NewGuid().ToString("N")[..8];
        var reference = $"{tenant.OrganizationId}:{invoice.Id}:{nonce}";

        var channel = cmd.Channel.ToLower() switch
        {
            "mpesa" => PaymentChannel.Mpesa,
            "card"  => PaymentChannel.Card,
            _       => PaymentChannel.Other
        };

        var isTechFee = invoice.FeeCode == FeeCode.Technology;
        var subaccount = isTechFee ? null : settings.SchoolSubaccountCode;

        var paystackReq = new PaystackInitRequest(
            Email: invoice.Application.User.Email,
            Amount: (long)(invoice.Amount * 100),
            Reference: reference,
            Channels: cmd.Channel.ToLower() == "mpesa" ? ["mobile_money"] : ["card"],
            Subaccount: subaccount,
            Bearer: subaccount != null ? "subaccount" : null);

        var result = await paystack.InitializeTransactionAsync(paystackReq);

        db.Payments.Add(new Payment
        {
            OrganizationId = tenant.OrganizationId,
            InvoiceId = invoice.Id,
            Reference = reference,
            Channel = channel,
            Status = PaymentStatus.Pending,
            AmountGross = invoice.Amount,
            AuthorizationUrl = result.AuthorizationUrl,
            ProviderRef = result.Reference
        });
        await db.SaveChangesAsync(ct);

        return new PaymentInitDto(Guid.NewGuid(), reference, result.AuthorizationUrl, "Pending");
    }
}
