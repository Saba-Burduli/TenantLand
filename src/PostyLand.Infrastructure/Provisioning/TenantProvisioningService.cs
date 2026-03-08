using Amazon.Route53;
using Amazon.Route53.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using PostyLand.Application.Common.Contexts;
using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;

namespace PostyLand.Infrastructure.Provisioning;

public sealed class TenantProvisioningService(
    IOptions<ProvisioningOptions> options,
    IAmazonRoute53 route53Client,
    IAmazonS3 s3Client,
    ILogger<TenantProvisioningService> logger) : ITenantProvisioningService
{
    public async Task CreateDatabaseAsync(TenantContext tenantContext, CancellationToken cancellationToken)
    {
        await using var adminConnection = new NpgsqlConnection(options.Value.PostgresAdminConnectionString);
        await adminConnection.OpenAsync(cancellationToken);

        var tenantBuilder = new NpgsqlConnectionStringBuilder(tenantContext.DecryptedConnectionString);
        var databaseName = tenantBuilder.Database;
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("Tenant connection string does not contain a database name.");
        }

        await using var existsCommand = adminConnection.CreateCommand();
        existsCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @dbName";
        existsCommand.Parameters.AddWithValue("@dbName", databaseName);
        var exists = await existsCommand.ExecuteScalarAsync(cancellationToken) is not null;

        if (exists)
        {
            return;
        }

        await using var createCommand = adminConnection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        await createCommand.ExecuteNonQueryAsync(cancellationToken);
        logger.LogInformation("Database created for tenant {TenantId}", tenantContext.TenantId);
    }

    public async Task ConfigureDnsAsync(TenantContext tenantContext, CancellationToken cancellationToken)
    {
        if (options.Value.DisableExternalProvisioning || string.IsNullOrWhiteSpace(options.Value.HostedZoneId))
        {
            logger.LogInformation(
                "DNS provisioning skipped for tenant {TenantId}. DisableExternalProvisioning={DisableExternalProvisioning}, HostedZoneConfigured={HostedZoneConfigured}",
                tenantContext.TenantId,
                options.Value.DisableExternalProvisioning,
                !string.IsNullOrWhiteSpace(options.Value.HostedZoneId));
            return;
        }

        var subdomain = $"{tenantContext.Subdomain}.{options.Value.RootDomain}".TrimEnd('.');
        var request = new ChangeResourceRecordSetsRequest
        {
            HostedZoneId = options.Value.HostedZoneId,
            ChangeBatch = new ChangeBatch
            {
                Changes =
                [
                    new Change
                    {
                        Action = ChangeAction.UPSERT,
                        ResourceRecordSet = new ResourceRecordSet
                        {
                            Name = subdomain,
                            Type = RRType.CNAME,
                            TTL = 300,
                            ResourceRecords =
                            [
                                new ResourceRecord { Value = options.Value.DnsRecordTarget }
                            ]
                        }
                    }
                ]
            }
        };

        await route53Client.ChangeResourceRecordSetsAsync(request, cancellationToken);
        logger.LogInformation("DNS configured for tenant {TenantId}", tenantContext.TenantId);
    }

    public async Task CreateBucketAsync(TenantContext tenantContext, CancellationToken cancellationToken)
    {
        if (options.Value.DisableExternalProvisioning)
        {
            logger.LogInformation(
                "S3 bucket provisioning skipped for tenant {TenantId}. DisableExternalProvisioning=true",
                tenantContext.TenantId);
            return;
        }

        var bucketName = $"{options.Value.BucketPrefix}-{tenantContext.Subdomain}".ToLowerInvariant();
        var bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(
            s3Client,
            bucketName);

        if (bucketExists)
        {
            return;
        }

        await s3Client.PutBucketAsync(new PutBucketRequest
        {
            BucketName = bucketName
        }, cancellationToken);

        logger.LogInformation("Bucket created for tenant {TenantId}", tenantContext.TenantId);
    }
}


