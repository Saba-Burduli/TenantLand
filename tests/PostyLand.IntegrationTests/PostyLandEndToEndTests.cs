using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PostyLand.Application.Common.Interfaces;
using PostyLand.Application.Features.Tenants;
using PostyLand.Domain.Enums;
using PostyLand.IntegrationTests.Infrastructure;
using PostyLand.Persistence.Context;
using PostyLand.Persistence.Context.TenantModels;

namespace PostyLand.IntegrationTests;

[Collection(IntegrationCollection.Name)]
public sealed class PostyLandEndToEndTests(PostyLandIntegrationFixture fixture)
{
    [Fact]
    public async Task StartupAndDependencyInjection_ShouldBeHealthy()
    {
        var response = await fixture.Client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body);
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
        Assert.Equal(fixture.JwtIssuer, fixture.GetRuntimeConfigurationValue("Jwt:Issuer"));
        Assert.Equal(fixture.JwtAudience, fixture.GetRuntimeConfigurationValue("Jwt:Audience"));
        Assert.Equal(fixture.JwtSigningKey, fixture.GetRuntimeConfigurationValue("Jwt:SigningKey"));

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ITenantResolverService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ITenantMigrationService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ITenantProvider>());
    }

    [Fact]
    public async Task TenantRegistrationAndOnboarding_ShouldCreateTenantDatabaseAndCompleteJob()
    {
        var tenant = await fixture.RegisterTenantAndWaitAsync();
        var databaseExists = await fixture.DatabaseExistsAsync(tenant.TenantDatabase);
        string? jobState = null;
        for (var i = 0; i < 40; i++)
        {
            jobState = await fixture.GetHangfireJobStateAsync(tenant.OnboardingJobId);
            if (string.Equals(jobState, "Succeeded", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            await Task.Delay(250);
        }

        Assert.True(databaseExists);
        Assert.Equal("Succeeded", jobState);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var record = await db.Tenants.AsNoTracking().SingleAsync(x => x.Id == tenant.TenantId);
        Assert.Equal(TenantOnboardingStatus.Completed, record.Status);
        Assert.True(record.IsActive);
    }

    [Fact]
    public async Task TenantResolutionMiddleware_ShouldValidateSubdomainAndSubscription()
    {
        var tenant = await fixture.RegisterTenantAndWaitAsync();

        var missingSubdomain = await fixture.Client.GetAsync("/api/not-found");
        Assert.Equal(HttpStatusCode.NotFound, missingSubdomain.StatusCode);

        var unknownReq = new HttpRequestMessage(HttpMethod.Get, "/api/not-found");
        unknownReq.Headers.Host = "unknown.postyland.com";
        var unknownResponse = await fixture.Client.SendAsync(unknownReq);
        var unknownBody = await unknownResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.NotFound, unknownResponse.StatusCode);
        Assert.Contains("Tenant was not found", unknownBody, StringComparison.OrdinalIgnoreCase);

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var subscription = await db.Subscriptions.SingleAsync(x => x.TenantId == tenant.TenantId);
            subscription.IsActive = false;
            await db.SaveChangesAsync();
        }

        var inactiveReq = new HttpRequestMessage(HttpMethod.Get, "/api/not-found");
        inactiveReq.Headers.Host = $"{tenant.Subdomain}.postyland.com";
        var inactiveResponse = await fixture.Client.SendAsync(inactiveReq);
        var inactiveBody = await inactiveResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Forbidden, inactiveResponse.StatusCode);
        Assert.Contains("Tenant subscription is not valid", inactiveBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task JwtScopeMiddleware_ShouldEnforceTenantMatchAndRequiredScope()
    {
        var tenant = await fixture.RegisterTenantAndWaitAsync();

        var validToken = CreateToken(Guid.NewGuid(), tenant.TenantId, role: "Owner", scope: "tenant.api");

        var goodRequest = new HttpRequestMessage(HttpMethod.Get, "/api/tenant/ping");
        goodRequest.Headers.Add("X-Tenant-Subdomain", tenant.Subdomain);
        goodRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var goodResponse = await fixture.Client.SendAsync(goodRequest);
        Assert.Equal(HttpStatusCode.OK, goodResponse.StatusCode);

        var mismatchToken = CreateToken(Guid.NewGuid(), Guid.NewGuid(), role: "Owner", scope: "tenant.api");

        var mismatchRequest = new HttpRequestMessage(HttpMethod.Get, "/api/tenant/ping");
        mismatchRequest.Headers.Add("X-Tenant-Subdomain", tenant.Subdomain);
        mismatchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", mismatchToken);
        var mismatchResponse = await fixture.Client.SendAsync(mismatchRequest);
        var mismatchBody = await mismatchResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Forbidden, mismatchResponse.StatusCode);
        Assert.Contains("JWT tenant does not match", mismatchBody, StringComparison.OrdinalIgnoreCase);

        var noScopeToken = CreateToken(Guid.NewGuid(), tenant.TenantId, role: "Owner", scope: null);

        var noScopeRequest = new HttpRequestMessage(HttpMethod.Get, "/api/tenant/ping");
        noScopeRequest.Headers.Add("X-Tenant-Subdomain", tenant.Subdomain);
        noScopeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", noScopeToken);
        var noScopeResponse = await fixture.Client.SendAsync(noScopeRequest);
        var noScopeBody = await noScopeResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Forbidden, noScopeResponse.StatusCode);
        Assert.Contains("missing required role/scope", noScopeBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminEndpoints_ShouldRequirePlatformAdminPolicy()
    {
        var tenant = await fixture.RegisterTenantAndWaitAsync();

        var nonAdminToken = CreateToken(Guid.NewGuid(), tenant.TenantId, role: "Owner", scope: "tenant.api");

        var ownerPing = new HttpRequestMessage(HttpMethod.Get, "/api/tenant/ping");
        ownerPing.Headers.Add("X-Tenant-Subdomain", tenant.Subdomain);
        ownerPing.Headers.Authorization = new AuthenticationHeaderValue("Bearer", nonAdminToken);
        var ownerPingResponse = await fixture.Client.SendAsync(ownerPing);
        Assert.Equal(HttpStatusCode.OK, ownerPingResponse.StatusCode);

        var deniedRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/tenants/{tenant.TenantId}/migrations/run");
        deniedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", nonAdminToken);
        var deniedResponse = await fixture.Client.SendAsync(deniedRequest);
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);

        var adminToken = CreateToken(Guid.NewGuid(), Guid.NewGuid(), role: "PlatformAdmin", scope: "platform.admin");

        var allowedRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/tenants/{tenant.TenantId}/migrations/run");
        allowedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var allowedResponse = await fixture.Client.SendAsync(allowedRequest);
        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
    }

    [Fact]
    public async Task TenantMigration_ShouldRemainIsolatedPerTenant()
    {
        var tenant1 = await fixture.RegisterTenantAndWaitAsync();
        var tenant2 = await fixture.RegisterTenantAndWaitAsync();

        var platformToken = CreateToken(Guid.NewGuid(), Guid.NewGuid(), role: "PlatformAdmin", scope: "platform.admin");

        var migrateRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/tenants/{tenant1.TenantId}/migrations/run");
        migrateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var migrateResponse = await fixture.Client.SendAsync(migrateRequest);
        Assert.Equal(HttpStatusCode.OK, migrateResponse.StatusCode);

        var migratedDb1 = await HasMigrationHistoryAsync(tenant1.TenantDatabase);
        var migratedDb2 = await HasMigrationHistoryAsync(tenant2.TenantDatabase);

        Assert.True(migratedDb1);
        Assert.False(migratedDb2);
    }

    [Fact]
    public async Task DynamicTenantDbContext_ShouldRouteToCorrectDatabaseAndAvoidCrossTenantLeakage()
    {
        var tenant1 = await fixture.RegisterTenantAndWaitAsync();
        var tenant2 = await fixture.RegisterTenantAndWaitAsync();

        var platformToken = CreateToken(Guid.NewGuid(), Guid.NewGuid(), role: "PlatformAdmin", scope: "platform.admin");

        await MigrateTenantAsync(tenant1.TenantId, platformToken);
        await MigrateTenantAsync(tenant2.TenantId, platformToken);

        await InsertTenantMarkerAsync(tenant1.Subdomain, "marker-tenant-1");
        await InsertTenantMarkerAsync(tenant2.Subdomain, "marker-tenant-2");

        var tenant1SeesTenant2 = await TenantMarkerCountAsync(tenant1.Subdomain, "marker-tenant-2");
        var tenant2SeesTenant1 = await TenantMarkerCountAsync(tenant2.Subdomain, "marker-tenant-1");

        Assert.Equal(0, tenant1SeesTenant2);
        Assert.Equal(0, tenant2SeesTenant1);
    }

    private async Task<bool> HasMigrationHistoryAsync(string databaseName)
    {
        await using var connection = new Npgsql.NpgsqlConnection(
            $"Host=127.0.0.1;Port=5432;Database={databaseName};Username=postgres;Password=postgres");
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "select 1 from information_schema.tables where table_schema='public' and table_name='__EFMigrationsHistory'";
        return await command.ExecuteScalarAsync() is not null;
    }

    private async Task MigrateTenantAsync(Guid tenantId, string platformToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/tenants/{tenantId}/migrations/run");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var response = await fixture.Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("MigrationCompleted", body, StringComparison.OrdinalIgnoreCase);
    }

    private async Task InsertTenantMarkerAsync(string subdomain, string markerName)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var resolver = scope.ServiceProvider.GetRequiredService<ITenantResolverService>();
        var provider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
        var tenantContext = await resolver.ResolveAsync(subdomain, CancellationToken.None);
        provider.Set(tenantContext);

        var tenantDb = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        tenantDb.MigrationMarkers.Add(new TenantMigrationMarker
        {
            Id = Guid.NewGuid(),
            Name = markerName,
            CreatedAt = DateTime.UtcNow
        });
        await tenantDb.SaveChangesAsync();
    }

    private async Task<int> TenantMarkerCountAsync(string subdomain, string markerName)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var resolver = scope.ServiceProvider.GetRequiredService<ITenantResolverService>();
        var provider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
        var tenantContext = await resolver.ResolveAsync(subdomain, CancellationToken.None);
        provider.Set(tenantContext);

        var tenantDb = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        return await tenantDb.MigrationMarkers.CountAsync(x => x.Name == markerName);
    }

    private string CreateToken(Guid userId, Guid tenantId, string role, string? scope)
    {
        return JwtTokenFactory.Create(
            fixture.GetRuntimeConfigurationValue("Jwt:SigningKey"),
            fixture.GetRuntimeConfigurationValue("Jwt:Issuer"),
            fixture.GetRuntimeConfigurationValue("Jwt:Audience"),
            userId,
            tenantId,
            role,
            scope);
    }
}
