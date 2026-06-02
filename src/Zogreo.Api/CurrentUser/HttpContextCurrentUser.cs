using System.Security.Claims;
using Zogreo.Application.Common.Interfaces;

namespace Zogreo.Api.CurrentUser;

public class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var v = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return v != null && Guid.TryParse(v, out var g) ? g : null;
        }
    }

    public string? Role => User?.FindFirstValue(ClaimTypes.Role);
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}

public class HttpContextTenantProvider : ITenantProvider
{
    private Guid _orgId;
    private Guid? _userId;
    private string? _role;
    private bool _resolved;

    public Guid OrganizationId
    {
        get { EnsureResolved(); return _orgId; }
    }

    public Guid? UserId
    {
        get { EnsureResolved(); return _userId; }
    }

    public string? UserRole
    {
        get { EnsureResolved(); return _role; }
    }

    public void SetTenant(Guid organizationId, Guid? userId, string? userRole)
    {
        _orgId = organizationId;
        _userId = userId;
        _role = userRole;
        _resolved = true;
    }

    private void EnsureResolved()
    {
        if (_resolved) return;
        // Resolved by TenantMiddleware before this is called;
        // if somehow not yet resolved, default to empty (will be resolved shortly)
    }
}
