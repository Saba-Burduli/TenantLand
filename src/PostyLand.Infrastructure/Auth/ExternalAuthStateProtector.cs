using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using PostyLand.Application.Common.Exceptions;
using PostyLand.Application.Features.ExternalAuth;

namespace PostyLand.Infrastructure.Auth;

public sealed class ExternalAuthStateProtector(IDataProtectionProvider dataProtectionProvider) : IExternalAuthStateProtector
{
    private readonly IDataProtector stateProtector = dataProtectionProvider.CreateProtector("PostyLand.ExternalAuth.State.v1");

    public string Protect(ExternalAuthStatePayload payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var protectedPayload = stateProtector.Protect(json);
        var bytes = Encoding.UTF8.GetBytes(protectedPayload);
        return Convert.ToBase64String(bytes);
    }

    public ExternalAuthStatePayload Unprotect(string protectedState)
    {
        try
        {
            var payloadBytes = Convert.FromBase64String(protectedState);
            var protectedPayload = Encoding.UTF8.GetString(payloadBytes);
            var json = stateProtector.Unprotect(protectedPayload);
            var payload = JsonSerializer.Deserialize<ExternalAuthStatePayload>(json);
            return payload ?? throw new ForbiddenException("OAuth state payload is invalid.");
        }
        catch (Exception)
        {
            throw new ForbiddenException("OAuth state validation failed.");
        }
    }
}
