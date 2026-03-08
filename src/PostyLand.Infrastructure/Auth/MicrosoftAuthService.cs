using System.Net.Http.Headers;
using System.Text.Json;
using FluentValidation;
using Microsoft.Extensions.Options;
using PostyLand.Application.Common.Exceptions;
using PostyLand.Application.Features.ExternalAuth;

namespace PostyLand.Infrastructure.Auth;

public sealed class MicrosoftAuthService(
    IHttpClientFactory httpClientFactory,
    IOptions<ExternalAuthOptions> options) : IMicrosoftAuthService
{
    private const string UserInfoEndpoint = "https://graph.microsoft.com/oidc/userinfo";

    public string GetAuthorizationUrl(string state)
    {
        var provider = options.Value.Microsoft;
        EnsureProviderConfiguration(provider);

        var endpoint = $"https://login.microsoftonline.com/{provider.TenantId}/oauth2/v2.0/authorize";
        var query = string.Join("&", new[]
        {
            $"client_id={Uri.EscapeDataString(provider.ClientId)}",
            "response_type=code",
            $"redirect_uri={Uri.EscapeDataString(provider.RedirectUri)}",
            $"scope={Uri.EscapeDataString("openid profile email")}",
            $"state={Uri.EscapeDataString(state)}"
        });

        return $"{endpoint}?{query}";
    }

    public async Task<ExternalAuthProviderUserProfile> GetUserProfileAsync(
        string authorizationCode,
        CancellationToken cancellationToken)
    {
        var provider = options.Value.Microsoft;
        EnsureProviderConfiguration(provider);

        var tokenEndpoint = $"https://login.microsoftonline.com/{provider.TenantId}/oauth2/v2.0/token";

        var httpClient = httpClientFactory.CreateClient();
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = authorizationCode,
            ["client_id"] = provider.ClientId,
            ["client_secret"] = provider.ClientSecret,
            ["redirect_uri"] = provider.RedirectUri,
            ["grant_type"] = "authorization_code",
            ["scope"] = "openid profile email"
        });

        using var tokenResponse = await httpClient.PostAsync(tokenEndpoint, tokenRequest, cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            throw new ForbiddenException("Microsoft token exchange failed.");
        }

        using var tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(cancellationToken));
        var accessToken = tokenJson.RootElement.GetProperty("access_token").GetString();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ForbiddenException("Microsoft access token was not returned.");
        }

        using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
        userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var userInfoResponse = await httpClient.SendAsync(userInfoRequest, cancellationToken);
        if (!userInfoResponse.IsSuccessStatusCode)
        {
            throw new ForbiddenException("Microsoft user profile retrieval failed.");
        }

        using var userJson = JsonDocument.Parse(await userInfoResponse.Content.ReadAsStringAsync(cancellationToken));
        var providerUserId = userJson.RootElement.GetProperty("sub").GetString() ?? string.Empty;
        var email = userJson.RootElement.TryGetProperty("email", out var emailElement)
            ? emailElement.GetString() ?? string.Empty
            : string.Empty;
        if (string.IsNullOrWhiteSpace(email) && userJson.RootElement.TryGetProperty("preferred_username", out var preferredUsername))
        {
            email = preferredUsername.GetString() ?? string.Empty;
        }

        var name = userJson.RootElement.TryGetProperty("name", out var nameElement)
            ? nameElement.GetString() ?? string.Empty
            : string.Empty;

        if (string.IsNullOrWhiteSpace(providerUserId) || string.IsNullOrWhiteSpace(email))
        {
            throw new ForbiddenException("Microsoft profile does not include required identity fields.");
        }

        return new ExternalAuthProviderUserProfile
        {
            ProviderUserId = providerUserId,
            Email = email,
            Name = name,
            IsEmailVerified = true
        };
    }

    private static void EnsureProviderConfiguration(MicrosoftOAuthProviderOptions provider)
    {
        if (string.IsNullOrWhiteSpace(provider.ClientId) ||
            string.IsNullOrWhiteSpace(provider.ClientSecret) ||
            string.IsNullOrWhiteSpace(provider.RedirectUri) ||
            string.IsNullOrWhiteSpace(provider.TenantId))
        {
            throw new ValidationException("Microsoft OAuth options are not configured.");
        }
    }
}
