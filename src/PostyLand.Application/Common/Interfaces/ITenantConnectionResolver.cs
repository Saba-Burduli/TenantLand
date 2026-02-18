namespace PostyLand.Application.Common.Interfaces;

public interface ITenantConnectionResolver
{
    string Encrypt(string plainConnectionString);
    string ResolveDecryptedConnectionString(string encryptedConnectionString);
}
