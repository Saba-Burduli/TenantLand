using Microsoft.Extensions.Options;
using Npgsql;
using PostyLand.Application.Common.Interfaces;

namespace PostyLand.Infrastructure.MultiTenancy;

public sealed class TenantConnectionStringBuilder(IOptions<TenantDatabaseOptions> options) : ITenantConnectionStringBuilder
{
    public string Build(string subdomain)
    {
        var databaseName = $"tenant_{subdomain.Replace("-", "_").ToLowerInvariant()}";
        var builder = new NpgsqlConnectionStringBuilder(options.Value.Template)
        {
            Database = databaseName
        };

        return builder.ConnectionString;
    }
}
