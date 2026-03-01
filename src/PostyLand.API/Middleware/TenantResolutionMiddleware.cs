using Microsoft.Extensions.Options;
using PostyLand.API.Logging;
using PostyLand.Application.Common.Exceptions;
using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;
using PostyLand.Application.Features.Tenants;

namespace PostyLand.API.Middleware;

public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    IOptions<TenantResolutionOptions> options)
{
    public async Task InvokeAsync(
        HttpContext context,
        ITenantResolverService tenantResolverService,
        ITenantProvider tenantProvider)
    {
        if (ShouldBypass(context.Request.Path))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(options.Value.SubdomainHeader, out var explicitSubdomain) ||
            string.IsNullOrWhiteSpace(explicitSubdomain))
        {
            throw new NotFoundException("Tenant subdomain was not found.");
        }
        var subdomain = explicitSubdomain.ToString().ToLowerInvariant();

        var tenantContext = await tenantResolverService.ResolveAsync(subdomain, context.RequestAborted);
        tenantProvider.Set(tenantContext);
        context.Items[HttpContextItemKeys.TenantId] = tenantContext.TenantId;

        await next(context);
    }

    private bool ShouldBypass(PathString path)
    {
        return options.Value.BypassPathPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
    }
}


