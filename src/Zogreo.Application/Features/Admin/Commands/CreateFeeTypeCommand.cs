using FluentValidation;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;
using Zogreo.Domain.Entities;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Admin.Commands;

public record CreateFeeTypeCommand(FeeCode Code, string Name, decimal Amount, bool Refundable)
    : ICommand<FeeTypeDto>;

public class CreateFeeTypeCommandValidator : AbstractValidator<CreateFeeTypeCommand>
{
    public CreateFeeTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public class CreateFeeTypeCommandHandler(
    IApplicationDbContext db,
    ITenantProvider tenant) : ICommandHandler<CreateFeeTypeCommand, FeeTypeDto>
{
    public async Task<FeeTypeDto> Handle(CreateFeeTypeCommand cmd, CancellationToken ct)
    {
        var ft = new FeeType
        {
            OrganizationId = tenant.OrganizationId,
            Code = cmd.Code,
            Name = cmd.Name,
            Amount = cmd.Amount,
            Refundable = cmd.Refundable,
            Active = true
        };
        db.FeeTypes.Add(ft);
        await db.SaveChangesAsync(ct);
        return new FeeTypeDto(ft.Id, ft.Code.ToString(), ft.Name, ft.Amount, ft.Refundable, ft.Active);
    }
}
