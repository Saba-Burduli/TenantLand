namespace PostyLand.Infrastructure.Auth;

public sealed class ExternalAuthOptions
{
    public const string SectionName = "ExternalAuth";

    public OAuthProviderOptions Google { get; init; } = new();
    public MicrosoftOAuthProviderOptions Microsoft { get; init; } = new();
    public string FrontendRedirectUri { get; init; } = "http://localhost:3000/auth/callback";
    public int StateTtlMinutes { get; init; } = 10;
}

public class OAuthProviderOptions
{
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string RedirectUri { get; init; } = string.Empty;
}

public sealed class MicrosoftOAuthProviderOptions : OAuthProviderOptions
{
    public string TenantId { get; init; } = "common";
}
