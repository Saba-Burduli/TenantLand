namespace PostyLand.Application.Common.Interfaces.TenantInterfaces;

public interface ITenantOnboardingJobClient
{
    string Enqueue(Guid tenantId);
}

