using PostyLand.Domain.Enums;

namespace PostyLand.Domain.Entities;

public sealed class Subscription
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Plan { get; set; } = string.Empty;
    public int EmployeeLimit { get; set; }
    public BillingStatus BillingStatus { get; set; } = BillingStatus.Active;
    public DateTime RenewalDate { get; set; }
    public bool IsActive { get; set; } = true;

    public Tenant? Tenant { get; set; }
}
