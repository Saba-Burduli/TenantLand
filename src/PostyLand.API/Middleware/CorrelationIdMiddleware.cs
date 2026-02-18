using PostyLand.API.Logging;

namespace PostyLand.API.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var values)
            ? values.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[HttpContextItemKeys.CorrelationId] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        await next(context);
    }
}
