namespace PostyLand.Application.Common.Interfaces.TenantInterfaces;

public interface ITenantConnectionStringBuilder
{
    string Build(string subdomain);
}

