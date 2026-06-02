using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Admin.Commands.Users;

public record UpdateUserCommand(Guid Id, Role? Role, bool? Active) : ICommand<AdminUserDto>;

public class UpdateUserCommandHandler(IApplicationDbContext db)
    : ICommandHandler<UpdateUserCommand, AdminUserDto>
{
    public async Task<AdminUserDto> Handle(UpdateUserCommand cmd, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == cmd.Id, ct)
            ?? throw AppException.NotFound("User not found.");

        if (cmd.Role.HasValue) user.Role = cmd.Role.Value;
        if (cmd.Active.HasValue) user.Active = cmd.Active.Value;
        await db.SaveChangesAsync(ct);

        return new AdminUserDto(user.Id, user.FullName, user.Email, user.Phone, user.PhoneVerified, user.Role.ToString(), user.Active, user.CreatedAt);
    }
}
