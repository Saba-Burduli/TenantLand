namespace PostyLand.Application.Common.Interfaces.TenantInterfaces;

public interface ITenantConnectionResolver
{
    string Encrypt(string plainConnectionString);
    string ResolveDecryptedConnectionString(string encryptedConnectionString);
}

