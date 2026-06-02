using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Payments.Commands;
using Zogreo.Domain.Enums;

namespace Zogreo.Infrastructure.Jobs;

public class PaymentSweepJob(IApplicationDbContext db, ISender sender, ILogger<PaymentSweepJob> logger)
{
    public async Task RunAsync()
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-2);
        var pending = await db.Payments.IgnoreQueryFilters()
            .Where(p => p.Status == PaymentStatus.Pending && p.CreatedAt < cutoff)
            .Select(p => p.Reference)
            .ToListAsync();

        foreach (var reference in pending)
        {
            try { await sender.Send(new ApplyPaymentConfirmationCommand(reference)); }
            catch (Exception ex) { logger.LogError(ex, "Sweep failed for {Ref}", reference); }
        }
    }
}
