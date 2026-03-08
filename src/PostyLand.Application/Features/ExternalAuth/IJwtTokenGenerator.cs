using PostyLand.Domain.Enums;

namespace PostyLand.Application.Features.ExternalAuth;

public interface IJwtTokenGenerator
{
    string CreateTenantToken(Guid userId, Guid tenantId, RoleStatus role, string scope);
}
