namespace PostyLand.Application.Common.Interfaces;

public interface ITenantOnboardingJobClient
{
    string Enqueue(Guid tenantId);
}
