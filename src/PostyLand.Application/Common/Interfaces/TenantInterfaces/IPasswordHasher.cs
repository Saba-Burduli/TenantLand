namespace PostyLand.Application.Common.Interfaces.TenantInterfaces;

public interface IPasswordHasher
{
    string Hash(string input);
}

