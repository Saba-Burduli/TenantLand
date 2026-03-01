using PostyLand.Domain.Enums;

namespace PostyLand.Application.Features.ExternalAuth;

public interface IExternalAuthService
{
    string GetLoginUrl(ExternalAuthProvider provider, string subdomain, string? returnUrl = null);

    Task<ExternalAuthCallbackResult> HandleCallbackAsync(
        ExternalAuthProvider provider,
        string code,
        string state,
        CancellationToken cancellationToken);
}
