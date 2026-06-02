using Zogreo.Domain.Common;
using Zogreo.Domain.Enums;

namespace Zogreo.Domain.Entities;

public class User : TenantEntity
{
    public Role Role { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool PhoneVerified { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
}
