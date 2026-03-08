namespace PostyLand.Application.Features.ExternalAuth;

public interface IMicrosoftAuthService
{
    string GetAuthorizationUrl(string state);

    Task<ExternalAuthProviderUserProfile> GetUserProfileAsync(string authorizationCode, CancellationToken cancellationToken);
}
