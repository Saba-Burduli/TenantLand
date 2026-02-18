using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace PostyLand.API.Logging;

public sealed class RequestContextEnricher(IHttpContextAccessor httpContextAccessor) : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var context = httpContextAccessor.HttpContext;
        var correlationId = context?.Items[HttpContextItemKeys.CorrelationId]?.ToString() ?? string.Empty;
        var tenantId = context?.Items[HttpContextItemKeys.TenantId]?.ToString() ?? string.Empty;
        var userId = context?.Items[HttpContextItemKeys.UserId]?.ToString() ?? string.Empty;

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", correlationId));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TenantId", tenantId));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", userId));
    }
}
