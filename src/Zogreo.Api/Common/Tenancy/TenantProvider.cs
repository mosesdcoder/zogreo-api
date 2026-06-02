namespace Zogreo.Api.Common.Tenancy;

public class TenantProvider : ITenantProvider
{
    public Guid OrganizationId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? UserRole { get; private set; }

    public void SetTenant(Guid organizationId, Guid? userId, string? userRole)
    {
        OrganizationId = organizationId;
        UserId = userId;
        UserRole = userRole;
    }
}
