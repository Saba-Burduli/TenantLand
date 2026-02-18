namespace PostyLand.Domain.Enums;

public enum TenantOnboardingStatus
{
    Pending = 0,
    DatabaseCreated = 1,
    DnsConfigured = 2,
    BucketCreated = 3,
    Completed = 4,
    Failed = 5
}
