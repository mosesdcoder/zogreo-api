using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Auth.DTOs;

namespace Zogreo.Application.Features.Auth.Commands;

public record UpdateProfileCommand(string? FullName, string? Phone) : ICommand<UserProfileDto>;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        When(x => x.FullName != null, () => RuleFor(x => x.FullName).NotEmpty());
        When(x => x.Phone != null, () => RuleFor(x => x.Phone).NotEmpty());
    }
}

public class UpdateProfileCommandHandler(
    IApplicationDbContext db,
    ITenantProvider tenant) : ICommandHandler<UpdateProfileCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(UpdateProfileCommand cmd, CancellationToken ct)
    {
        var userId = tenant.UserId ?? throw AppException.Unauthorized();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw AppException.NotFound("User not found.");

        if (cmd.FullName != null) user.FullName = cmd.FullName;
        if (cmd.Phone != null) user.Phone = cmd.Phone;

        await db.SaveChangesAsync(ct);
        return new UserProfileDto(user.Id, user.FullName, user.Email, user.Phone, user.PhoneVerified, user.Role.ToString());
    }
}
