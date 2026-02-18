namespace PostyLand.API.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string SigningKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
}
