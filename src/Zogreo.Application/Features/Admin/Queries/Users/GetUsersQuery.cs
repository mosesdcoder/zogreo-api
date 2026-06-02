using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Admin.Queries.Users;

public record GetUsersQuery(string? Role, bool? Active, int Page = 1, int PageSize = 20) : IQuery<object>;

public class GetUsersQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetUsersQuery, object>
{
    public async Task<object> Handle(GetUsersQuery q, CancellationToken ct)
    {
        var query = db.Users.AsNoTracking().AsQueryable();
        if (q.Role != null && Enum.TryParse<Role>(q.Role, true, out var role))
            query = query.Where(u => u.Role == role);
        if (q.Active.HasValue) query = query.Where(u => u.Active == q.Active.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.FullName)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(u => new AdminUserDto(u.Id, u.FullName, u.Email, u.Phone, u.PhoneVerified, u.Role.ToString(), u.Active, u.CreatedAt))
            .ToListAsync(ct);

        return new { total, page = q.Page, items };
    }
}
