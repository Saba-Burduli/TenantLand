using Amazon;
using Amazon.Route53;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;
using PostyLand.Infrastructure.Auth;
using PostyLand.Infrastructure.BackgroundJobs;
using PostyLand.Infrastructure.MultiTenancy;
using PostyLand.Infrastructure.Provisioning;

namespace PostyLand.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EncryptionOptions>(configuration.GetSection(EncryptionOptions.SectionName));
        services.Configure<TenantDatabaseOptions>(configuration.GetSection(TenantDatabaseOptions.SectionName));
        services.Configure<ProvisioningOptions>(configuration.GetSection(ProvisioningOptions.SectionName));

        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddScoped<IUserContextProvider, UserContextProvider>();
        services.AddScoped<ITenantConnectionResolver, AesTenantConnectionResolver>();
        services.AddScoped<ITenantConnectionStringBuilder, TenantConnectionStringBuilder>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<ITenantOnboardingJobClient, TenantOnboardingJobClient>();
        services.AddScoped<TenantOnboardingJob>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();

        services.AddSingleton<IAmazonS3>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ProvisioningOptions>>().Value;
            return new AmazonS3Client(RegionEndpoint.GetBySystemName(options.AwsRegion));
        });

        services.AddSingleton<IAmazonRoute53>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ProvisioningOptions>>().Value;
            return new AmazonRoute53Client(RegionEndpoint.GetBySystemName(options.AwsRegion));
        });

        return services;
    }
}


