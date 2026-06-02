namespace Zogreo.Api.Common.Tenancy;

public interface ITenantProvider
{
    Guid OrganizationId { get; }
    Guid? UserId { get; }
    string? UserRole { get; }
    void SetTenant(Guid organizationId, Guid? userId, string? userRole);
}
