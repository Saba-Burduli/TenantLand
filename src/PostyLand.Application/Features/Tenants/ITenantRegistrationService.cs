namespace PostyLand.Application.Features.Tenants;

public interface ITenantRegistrationService
{
    Task<RegisterTenantResponse> RegisterAsync(RegisterTenantRequest request, CancellationToken cancellationToken);
}
