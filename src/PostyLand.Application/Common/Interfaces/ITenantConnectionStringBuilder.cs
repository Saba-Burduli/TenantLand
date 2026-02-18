namespace PostyLand.Application.Common.Interfaces;

public interface ITenantConnectionStringBuilder
{
    string Build(string subdomain);
}
