using Hangfire;
using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;

namespace PostyLand.Infrastructure.BackgroundJobs;

public sealed class TenantOnboardingJobClient(IBackgroundJobClient backgroundJobClient) : ITenantOnboardingJobClient
{
    public string Enqueue(Guid tenantId)
    {
        return backgroundJobClient.Enqueue<TenantOnboardingJob>(
            job => job.RunAsync(tenantId, CancellationToken.None));
    }
}


