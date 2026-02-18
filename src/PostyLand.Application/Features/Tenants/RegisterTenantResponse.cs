using PostyLand.Domain.Enums;

namespace PostyLand.Application.Features.Tenants;

public sealed class RegisterTenantResponse
{
    public Guid TenantId { get; init; }
    public string OnboardingJobId { get; init; } = string.Empty;
    public TenantOnboardingStatus Status { get; init; }
}
