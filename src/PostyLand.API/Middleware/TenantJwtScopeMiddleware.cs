using System.Security.Claims;
using PostyLand.API.Logging;
using PostyLand.Application.Common.Contexts;
using PostyLand.Application.Common.Exceptions;
using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;
using PostyLand.Domain.Enums;

namespace PostyLand.API.Middleware;

public sealed class TenantJwtScopeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider, IUserContextProvider userContextProvider)
    {
        if (context.User.Identity?.IsAuthenticated is true)
        {
            var userIdClaim = context.User.FindFirst("UserId")?.Value
                              ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? context.User.FindFirst("sub")?.Value;
            var tenantIdClaim = context.User.FindFirst("TenantId")?.Value
                                ?? context.User.FindFirst("tenant_id")?.Value;
            var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value
                            ?? context.User.FindFirst("Role")?.Value
                            ?? string.Empty;
            var scopeClaim = context.User.FindFirst("Scope")?.Value
                             ?? context.User.FindFirst("scope")?.Value
                             ?? string.Empty;

            if (!Guid.TryParse(userIdClaim, out var userId) || !Guid.TryParse(tenantIdClaim, out var tokenTenantId))
            {
                throw new ForbiddenException("JWT is missing required tenant/user claims.");
            }

            if (string.IsNullOrWhiteSpace(roleClaim) || string.IsNullOrWhiteSpace(scopeClaim))
            {
                throw new ForbiddenException("JWT is missing required role/scope claims.");
            }

            if (!Enum.TryParse<RoleStatus>(roleClaim, true, out var roleStatus) || roleStatus == RoleStatus.Unknown)
            {
                throw new ForbiddenException("JWT contains unsupported role.");
            }

            var resolvedTenant = tenantProvider.Current;
            if (resolvedTenant is not null && resolvedTenant.TenantId != tokenTenantId)
            {
                throw new ForbiddenException("JWT tenant does not match resolved tenant.");
            }

            userContextProvider.Set(new UserContext
            {
                UserId = userId,
                TenantId = tokenTenantId,
                Subdomain = resolvedTenant?.Subdomain ?? string.Empty,
                Role = roleStatus,
                Scope = scopeClaim
            });

            context.Items[HttpContextItemKeys.UserId] = userId;
            context.Items[HttpContextItemKeys.TenantId] = tokenTenantId;
        }

        await next(context);
    }
}


