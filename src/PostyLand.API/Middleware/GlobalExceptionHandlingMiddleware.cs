using FluentValidation;
using PostyLand.Application.Common.Exceptions;
using PostyLand.Domain.Exceptions;

namespace PostyLand.API.Middleware;

public sealed class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var (statusCode, title) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
                ForbiddenException => (StatusCodes.Status403Forbidden, "Access denied"),
                ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
                ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
                DomainException => (StatusCodes.Status400BadRequest, "Domain error"),
                InfrastructureException => (StatusCodes.Status500InternalServerError, "Infrastructure error"),
                _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
            };

            logger.LogError(ex, "Unhandled exception");

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                title,
                detail = ex.Message,
                statusCode,
                correlationId = context.Items[Logging.HttpContextItemKeys.CorrelationId]?.ToString()
            });
        }
    }
}
