namespace PostyLand.Application.Features.Tenants;

public sealed class RegisterTenantRequest
{
    public string Name { get; init; } = string.Empty;
    public string Subdomain { get; init; } = string.Empty;
    public string AdminEmail { get; init; } = string.Empty;
    public string AdminPassword { get; init; } = string.Empty;
    public string Plan { get; init; } = string.Empty;
    public int EmployeeLimit { get; init; }
    public DateTime RenewalDate { get; init; }
}
