using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PostyLand.Application;
using PostyLand.Application.Common.Interfaces;
using PostyLand.Application.Features.Tenants;
using PostyLand.Infrastructure;
using PostyLand.Persistence;
using PostyLand.Persistence.Context;
using PostyLand.Persistence.Context.TenantModels;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "PostyLand.API"))
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build();

var services = new ServiceCollection();
services.AddApplication();
services.AddPersistence(configuration);
services.AddInfrastructure(configuration);
var serviceProvider = services.BuildServiceProvider();

var outputs = new List<object>();
foreach (var subdomain in new[] { "acme", "beta" })
{
    await using var scope = serviceProvider.CreateAsyncScope();
    var resolver = scope.ServiceProvider.GetRequiredService<ITenantResolverService>();
    var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
    var tenantContext = await resolver.ResolveAsync(subdomain, CancellationToken.None);
    tenantProvider.Set(tenantContext);

    var tenantDb = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
    var databaseName = tenantDb.Database.GetDbConnection().Database;

    var markerName = $"probe-{subdomain}";
    var existing = await tenantDb.MigrationMarkers.AnyAsync(x => x.Name == markerName);
    if (!existing)
    {
        tenantDb.MigrationMarkers.Add(new TenantMigrationMarker
        {
            Id = Guid.NewGuid(),
            Name = markerName,
            CreatedAt = DateTime.UtcNow
        });
        await tenantDb.SaveChangesAsync();
    }

    var markerCount = await tenantDb.MigrationMarkers.CountAsync();
    outputs.Add(new
    {
        subdomain,
        databaseName,
        markerCount
    });
}

var isolationChecks = new List<object>();
foreach (var entry in new[] { ("acme", "probe-beta"), ("beta", "probe-acme") })
{
    await using var scope = serviceProvider.CreateAsyncScope();
    var resolver = scope.ServiceProvider.GetRequiredService<ITenantResolverService>();
    var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
    var tenantContext = await resolver.ResolveAsync(entry.Item1, CancellationToken.None);
    tenantProvider.Set(tenantContext);
    var tenantDb = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
    var foreignMarkerCount = await tenantDb.MigrationMarkers.CountAsync(x => x.Name == entry.Item2);
    isolationChecks.Add(new
    {
        tenant = entry.Item1,
        forbiddenMarker = entry.Item2,
        foreignMarkerCount
    });
}

Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
{
    routedDatabases = outputs,
    isolationChecks
}));
