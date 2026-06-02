namespace Zogreo.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}

public interface ITenantProvider
{
    Guid OrganizationId { get; }
    Guid? UserId { get; }
    string? UserRole { get; }
    void SetTenant(Guid organizationId, Guid? userId, string? userRole);
}
