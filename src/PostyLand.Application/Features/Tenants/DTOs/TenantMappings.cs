using PostyLand.Domain.Entities;

namespace PostyLand.Application.Features.Tenants.DTOs;

public static class TenantMappings
{
    public static Tenant ToEntity(this CreateTenantRequest request, string normalizedSubdomain)
    {
        return new Tenant
        {
            Id = request.Id.GetValueOrDefault(Guid.NewGuid()),
            Name = request.Name.Trim(),
            Subdomain = normalizedSubdomain,
            EncryptedConnectionString = request.EncryptedConnectionString.Trim(),
            IsActive = request.IsActive,
            CreatedAt = request.CreatedAt?.ToUniversalTime() ?? DateTime.UtcNow
        };
    }

    public static void ApplyTo(this UpdateTenantRequest request, Tenant tenant, string normalizedSubdomain)
    {
        tenant.Name = request.Name.Trim();
        tenant.Subdomain = normalizedSubdomain;
        tenant.EncryptedConnectionString = request.EncryptedConnectionString.Trim();
        tenant.IsActive = request.IsActive;

        if (request.CreatedAt.HasValue)
        {
            tenant.CreatedAt = request.CreatedAt.Value.ToUniversalTime();
        }
    }

    public static TenantResponse ToResponse(this Tenant tenant)
    {
        return new TenantResponse
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Subdomain = tenant.Subdomain,
            EncryptedConnectionString = tenant.EncryptedConnectionString,
            IsActive = tenant.IsActive,
            CreatedAt = tenant.CreatedAt
        };
    }
}
