using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace PostyLand.IntegrationTests.Infrastructure;

public sealed class PostyLandApiFactory : WebApplicationFactory<Program>
{
    private readonly IReadOnlyDictionary<string, string?> _configOverrides;

    public PostyLandApiFactory(IReadOnlyDictionary<string, string?> configOverrides)
    {
        _configOverrides = configOverrides;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(_configOverrides);
        });
    }
}
