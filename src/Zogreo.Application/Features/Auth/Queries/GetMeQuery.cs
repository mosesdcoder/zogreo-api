using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Auth.DTOs;

namespace Zogreo.Application.Features.Auth.Queries;

public record GetMeQuery : IQuery<UserProfileDto>;

public class GetMeQueryHandler(
    IApplicationDbContext db,
    ITenantProvider tenant) : IQueryHandler<GetMeQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(GetMeQuery q, CancellationToken ct)
    {
        var userId = tenant.UserId ?? throw AppException.Unauthorized();
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw AppException.NotFound("User not found.");

        return new UserProfileDto(user.Id, user.FullName, user.Email, user.Phone, user.PhoneVerified, user.Role.ToString());
    }
}
