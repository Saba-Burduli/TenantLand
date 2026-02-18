namespace PostyLand.Application.Features.Tenants;

public interface ITenantOnboardingOrchestrator
{
    Task ExecuteAsync(Guid tenantId, CancellationToken cancellationToken);
}
