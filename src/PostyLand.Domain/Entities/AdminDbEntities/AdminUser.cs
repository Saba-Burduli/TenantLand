using PostyLand.Domain.Enums;

namespace PostyLand.Domain.Entities;

public sealed class AdminUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public RoleStatus Role { get; set; } = RoleStatus.Admin;
}
