using PostyLand.Domain.Enums;

namespace PostyLand.Application.Features.ExternalAuth;

public sealed class ExternalAuthStatePayload
{
    public ExternalAuthProvider Provider { get; init; }
    public string Subdomain { get; init; } = string.Empty;
    public string Nonce { get; init; } = string.Empty;
    public DateTime IssuedAtUtc { get; init; }
    public string? ReturnUrl { get; init; }
}
