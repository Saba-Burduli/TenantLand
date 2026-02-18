namespace PostyLand.Infrastructure.Provisioning;

public sealed class ProvisioningOptions
{
    public const string SectionName = "Provisioning";
    public string PostgresAdminConnectionString { get; init; } = string.Empty;
    public string HostedZoneId { get; init; } = string.Empty;
    public string RootDomain { get; init; } = "postyland.com";
    public string DnsRecordTarget { get; init; } = string.Empty;
    public string AwsRegion { get; init; } = "us-east-1";
    public string BucketPrefix { get; init; } = "postyland";
    public bool DisableExternalProvisioning { get; init; }
}
