using System.Net.Http.Headers;
using System.Text.Json;
using FluentValidation;
using Microsoft.Extensions.Options;
using PostyLand.Application.Common.Exceptions;
using PostyLand.Application.Features.ExternalAuth;

namespace PostyLand.Infrastructure.Auth;

public sealed class GoogleAuthService(
    IHttpClientFactory httpClientFactory,
    IOptions<ExternalAuthOptions> options) : IGoogleAuthService
{
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string UserInfoEndpoint = "https://openidconnect.googleapis.com/v1/userinfo";

    public string GetAuthorizationUrl(string state)
    {
        var provider = options.Value.Google;
        EnsureProviderConfiguration(provider, "Google");

        var query = string.Join("&", new[]
        {
            $"client_id={Uri.EscapeDataString(provider.ClientId)}",
            "response_type=code",
            $"redirect_uri={Uri.EscapeDataString(provider.RedirectUri)}",
            $"scope={Uri.EscapeDataString("openid profile email")}",
            $"state={Uri.EscapeDataString(state)}"
        });

        return $"{AuthorizationEndpoint}?{query}";
    }

    public async Task<ExternalAuthProviderUserProfile> GetUserProfileAsync(
        string authorizationCode,
        CancellationToken cancellationToken)
    {
        var provider = options.Value.Google;
        EnsureProviderConfiguration(provider, "Google");

        var httpClient = httpClientFactory.CreateClient();
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = authorizationCode,
            ["client_id"] = provider.ClientId,
            ["client_secret"] = provider.ClientSecret,
            ["redirect_uri"] = provider.RedirectUri,
            ["grant_type"] = "authorization_code"
        });

        using var tokenResponse = await httpClient.PostAsync(TokenEndpoint, tokenRequest, cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            throw new ForbiddenException("Google token exchange failed.");
        }

        using var tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(cancellationToken));
        var accessToken = tokenJson.RootElement.GetProperty("access_token").GetString();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ForbiddenException("Google access token was not returned.");
        }

        using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
        userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var userInfoResponse = await httpClient.SendAsync(userInfoRequest, cancellationToken);
        if (!userInfoResponse.IsSuccessStatusCode)
        {
            throw new ForbiddenException("Google user profile retrieval failed.");
        }

        using var userJson = JsonDocument.Parse(await userInfoResponse.Content.ReadAsStringAsync(cancellationToken));
        var providerUserId = userJson.RootElement.GetProperty("sub").GetString() ?? string.Empty;
        var email = userJson.RootElement.GetProperty("email").GetString() ?? string.Empty;
        var name = userJson.RootElement.TryGetProperty("name", out var nameElement)
            ? nameElement.GetString() ?? string.Empty
            : string.Empty;
        var isEmailVerified = userJson.RootElement.TryGetProperty("email_verified", out var emailVerifiedElement)
            && emailVerifiedElement.ValueKind == JsonValueKind.True;

        if (string.IsNullOrWhiteSpace(providerUserId) || string.IsNullOrWhiteSpace(email))
        {
            throw new ForbiddenException("Google profile does not include required identity fields.");
        }

        return new ExternalAuthProviderUserProfile
        {
            ProviderUserId = providerUserId,
            Email = email,
            Name = name,
            IsEmailVerified = isEmailVerified
        };
    }

    private static void EnsureProviderConfiguration(OAuthProviderOptions provider, string providerName)
    {
        if (string.IsNullOrWhiteSpace(provider.ClientId) ||
            string.IsNullOrWhiteSpace(provider.ClientSecret) ||
            string.IsNullOrWhiteSpace(provider.RedirectUri))
        {
            throw new ValidationException($"{providerName} OAuth options are not configured.");
        }
    }
}
