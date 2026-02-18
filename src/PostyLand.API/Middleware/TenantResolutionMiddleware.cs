using Microsoft.Extensions.Options;
using PostyLand.API.Logging;
using PostyLand.Application.Common.Exceptions;
using PostyLand.Application.Common.Interfaces;
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

        var subdomain = TryExtractSubdomain(context.Request.Host.Host, context.Request.Headers);
        if (string.IsNullOrWhiteSpace(subdomain))
        {
            throw new NotFoundException("Tenant subdomain was not found.");
        }

        var tenantContext = await tenantResolverService.ResolveAsync(subdomain, context.RequestAborted);
        tenantProvider.Set(tenantContext);
        context.Items[HttpContextItemKeys.TenantId] = tenantContext.TenantId;

        await next(context);
    }

    private bool ShouldBypass(PathString path)
    {
        return options.Value.BypassPathPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private string? TryExtractSubdomain(string host, IHeaderDictionary headers)
    {
        if (headers.TryGetValue(options.Value.SubdomainHeader, out var explicitSubdomain) &&
            !string.IsNullOrWhiteSpace(explicitSubdomain))
        {
            return explicitSubdomain.ToString().ToLowerInvariant();
        }

        if (host.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var rootDomain = options.Value.RootDomain.ToLowerInvariant();
        var normalizedHost = host.ToLowerInvariant();
        if (!normalizedHost.EndsWith(rootDomain))
        {
            return null;
        }

        var prefix = normalizedHost[..^rootDomain.Length].TrimEnd('.');
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return null;
        }

        var subdomain = prefix.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(subdomain))
        {
            return null;
        }

        if (options.Value.ReservedSubdomains.Any(x => x.Equals(subdomain, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        return subdomain;
    }
}
