namespace PostyLand.Application.Features.ExternalAuth;

public interface IGoogleAuthService
{
    string GetAuthorizationUrl(string state);

    Task<ExternalAuthProviderUserProfile> GetUserProfileAsync(string authorizationCode, CancellationToken cancellationToken);
}
