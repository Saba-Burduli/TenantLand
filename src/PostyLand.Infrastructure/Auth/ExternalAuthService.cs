using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PostyLand.Application.Common.Exceptions;
using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Features.ExternalAuth;
using PostyLand.Application.Features.Tenants;
using PostyLand.Domain.Entities;
using PostyLand.Domain.Enums;

namespace PostyLand.Infrastructure.Auth;

public sealed class ExternalAuthService(
    IGoogleAuthService googleAuthService,
    IMicrosoftAuthService microsoftAuthService,
    IExternalAuthStateProtector stateProtector,
    IAdminUserStore adminUserStore,
    ITenantResolverService tenantResolverService,
    IJwtTokenGenerator jwtTokenGenerator,
    IOptions<ExternalAuthOptions> options,
    ILogger<ExternalAuthService> logger) : IExternalAuthService
{
    public string GetLoginUrl(ExternalAuthProvider provider, string subdomain, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
        {
            throw new ValidationException("Subdomain is required.");
        }

        var normalizedSubdomain = subdomain.Trim().ToLowerInvariant();
        var normalizedReturnUrl = NormalizeReturnUrl(returnUrl);
        var payload = new ExternalAuthStatePayload
        {
            Provider = provider,
            Subdomain = normalizedSubdomain,
            Nonce = Guid.NewGuid().ToString("N"),
            IssuedAtUtc = DateTime.UtcNow,
            ReturnUrl = normalizedReturnUrl
        };

        var state = stateProtector.Protect(payload);
        return provider switch
        {
            ExternalAuthProvider.Google => googleAuthService.GetAuthorizationUrl(state),
            ExternalAuthProvider.Microsoft => microsoftAuthService.GetAuthorizationUrl(state),
            _ => throw new ValidationException("Unsupported external auth provider.")
        };
    }

    public async Task<ExternalAuthCallbackResult> HandleCallbackAsync(
        ExternalAuthProvider provider,
        string code,
        string state,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return BuildErrorResult("missing_code");
        }

        if (string.IsNullOrWhiteSpace(state))
        {
            return BuildErrorResult("missing_state");
        }

        try
        {
            var payload = stateProtector.Unprotect(state);
            ValidateState(provider, payload);

            var tenantContext = await tenantResolverService.ResolveAsync(payload.Subdomain, cancellationToken);

            var profile = provider switch
            {
                ExternalAuthProvider.Google => await googleAuthService.GetUserProfileAsync(code, cancellationToken),
                ExternalAuthProvider.Microsoft => await microsoftAuthService.GetUserProfileAsync(code, cancellationToken),
                _ => throw new ValidationException("Unsupported external auth provider.")
            };

            if (!profile.IsEmailVerified)
            {
                return BuildErrorResult("email_not_verified");
            }

            var user = await GetOrCreateExternalUserAsync(provider, profile, cancellationToken);
            var token = jwtTokenGenerator.CreateTenantToken(user.Id, tenantContext.TenantId, user.Role, "tenant.api");
            return BuildSuccessResult(token, tenantContext.TenantId, tenantContext.Subdomain, payload.ReturnUrl);
        }
        catch (Exception ex) when (
            ex is ForbiddenException
            or ConflictException
            or ValidationException
            or NotFoundException)
        {
            logger.LogWarning(ex, "External auth callback failed for provider {Provider}", provider);
            return BuildErrorResult("oauth_failed");
        }
    }

    private static string? NormalizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return null;
        }

        var trimmed = returnUrl.Trim();
        return trimmed.StartsWith('/') ? trimmed : null;
    }

    private void ValidateState(ExternalAuthProvider provider, ExternalAuthStatePayload payload)
    {
        if (payload.Provider != provider)
        {
            throw new ForbiddenException("OAuth provider does not match callback state.");
        }

        var ttl = TimeSpan.FromMinutes(options.Value.StateTtlMinutes <= 0 ? 10 : options.Value.StateTtlMinutes);
        if (DateTime.UtcNow - payload.IssuedAtUtc > ttl)
        {
            throw new ForbiddenException("OAuth state has expired.");
        }
    }

    private async Task<AdminUser> GetOrCreateExternalUserAsync(
        ExternalAuthProvider provider,
        ExternalAuthProviderUserProfile profile,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = profile.Email.Trim().ToLowerInvariant();
        var providerUserId = profile.ProviderUserId.Trim();

        var user = await adminUserStore.GetByExternalProviderAsync(provider, providerUserId, cancellationToken);
        var changed = false;
        if (user is null)
        {
            user = await adminUserStore.GetByEmailAsync(normalizedEmail, cancellationToken);
            if (user is not null)
            {
                if (!user.IsExternalAccount)
                {
                    throw new ConflictException("Email is already registered with local credentials.");
                }

                if (user.ExternalProvider is not null && user.ExternalProvider != provider)
                {
                    throw new ConflictException("Email is already linked to another external provider.");
                }

                user.ExternalProvider = provider;
                user.ExternalProviderId = providerUserId;
                user.IsExternalAccount = true;
                changed = true;
            }
        }

        if (user is null)
        {
            user = new AdminUser
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                PasswordHash = string.Empty,
                Role = RoleStatus.Owner,
                ExternalProvider = provider,
                ExternalProviderId = providerUserId,
                IsExternalAccount = true
            };

            await adminUserStore.AddAsync(user, cancellationToken);
            await adminUserStore.SaveChangesAsync(cancellationToken);
            return user;
        }

        if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = normalizedEmail;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(user.ExternalProviderId))
        {
            user.ExternalProviderId = providerUserId;
            changed = true;
        }

        if (changed)
        {
            await adminUserStore.SaveChangesAsync(cancellationToken);
        }

        return user;
    }

    private ExternalAuthCallbackResult BuildSuccessResult(
        string token,
        Guid tenantId,
        string subdomain,
        string? returnUrl)
    {
        var queryParts = new List<string>
        {
            $"token={Uri.EscapeDataString(token)}",
            $"tenantId={Uri.EscapeDataString(tenantId.ToString())}",
            $"subdomain={Uri.EscapeDataString(subdomain)}"
        };

        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            queryParts.Add($"returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        var fragment = string.Join("&", queryParts);
        var redirectBase = options.Value.FrontendRedirectUri.TrimEnd('/');
        return new ExternalAuthCallbackResult
        {
            RedirectUrl = $"{redirectBase}#{fragment}"
        };
    }

    private ExternalAuthCallbackResult BuildErrorResult(string errorCode)
    {
        var redirectBase = options.Value.FrontendRedirectUri.TrimEnd('/');
        return new ExternalAuthCallbackResult
        {
            RedirectUrl = $"{redirectBase}#error={Uri.EscapeDataString(errorCode)}"
        };
    }
}
