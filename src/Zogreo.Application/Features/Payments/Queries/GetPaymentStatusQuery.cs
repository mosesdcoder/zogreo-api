using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Payments.Commands;
using Zogreo.Application.Features.Payments.DTOs;

namespace Zogreo.Application.Features.Payments.Queries;

public record GetPaymentStatusQuery(string Reference) : IQuery<PaymentStatusDto>;

public class GetPaymentStatusQueryHandler(
    IApplicationDbContext db,
    ISender sender) : IQueryHandler<GetPaymentStatusQuery, PaymentStatusDto>
{
    public async Task<PaymentStatusDto> Handle(GetPaymentStatusQuery q, CancellationToken ct)
    {
        var payment = await db.Payments.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Reference == q.Reference, ct)
            ?? throw AppException.NotFound($"Payment with reference '{q.Reference}' not found.");

        if (payment.Status == Domain.Enums.PaymentStatus.Pending)
        {
            var confirmed = await sender.Send(new ApplyPaymentConfirmationCommand(q.Reference), ct);
            return confirmed ?? throw AppException.NotFound("Payment could not be verified.");
        }

        return new PaymentStatusDto(payment.Id, payment.Reference, payment.Status.ToString(),
            payment.AmountGross, payment.ProviderFee, payment.TechnologyFee,
            payment.AmountNetToSchool, payment.CompletedAt);
    }
}
