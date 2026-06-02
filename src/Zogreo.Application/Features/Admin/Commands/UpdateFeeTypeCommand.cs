using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;

namespace Zogreo.Application.Features.Admin.Commands;

public record UpdateFeeTypeCommand(Guid Id, decimal Amount, bool? Active) : ICommand<FeeTypeDto>;

public class UpdateFeeTypeCommandHandler(IApplicationDbContext db)
    : ICommandHandler<UpdateFeeTypeCommand, FeeTypeDto>
{
    public async Task<FeeTypeDto> Handle(UpdateFeeTypeCommand cmd, CancellationToken ct)
    {
        var ft = await db.FeeTypes.FirstOrDefaultAsync(f => f.Id == cmd.Id, ct)
            ?? throw AppException.NotFound("Fee type not found.");

        ft.Amount = cmd.Amount;
        if (cmd.Active.HasValue) ft.Active = cmd.Active.Value;
        await db.SaveChangesAsync(ct);

        return new FeeTypeDto(ft.Id, ft.Code.ToString(), ft.Name, ft.Amount, ft.Refundable, ft.Active);
    }
}
