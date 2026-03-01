namespace PostyLand.Application.Features.ExternalAuth;

public sealed class ExternalAuthCallbackResult
{
    public string RedirectUrl { get; init; } = string.Empty;
}
