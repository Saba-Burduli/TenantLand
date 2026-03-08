namespace PostyLand.Application.Features.ExternalAuth;

public sealed class ExternalAuthProviderUserProfile
{
    public string ProviderUserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsEmailVerified { get; init; }
}
