using PostyLand.Application.Features.Tenants;

namespace PostyLand.Infrastructure.BackgroundJobs;

public sealed class TenantOnboardingJob(ITenantOnboardingOrchestrator orchestrator)
{
    public Task RunAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return orchestrator.ExecuteAsync(tenantId, cancellationToken);
    }
}
