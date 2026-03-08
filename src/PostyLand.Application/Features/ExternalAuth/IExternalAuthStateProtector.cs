namespace PostyLand.Application.Features.ExternalAuth;

public interface IExternalAuthStateProtector
{
    string Protect(ExternalAuthStatePayload payload);

    ExternalAuthStatePayload Unprotect(string protectedState);
}
