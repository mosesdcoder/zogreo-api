using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;

namespace Zogreo.Application.Features.Admin.Queries.Users;

public record GetUserQuery(Guid Id) : IQuery<AdminUserDto>;

public class GetUserQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetUserQuery, AdminUserDto>
{
    public async Task<AdminUserDto> Handle(GetUserQuery q, CancellationToken ct)
        => await db.Users.AsNoTracking()
            .Where(u => u.Id == q.Id)
            .Select(u => new AdminUserDto(u.Id, u.FullName, u.Email, u.Phone, u.PhoneVerified, u.Role.ToString(), u.Active, u.CreatedAt))
            .FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound("User not found.");
}
