using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;
using PostyLand.Domain.Enums;
using PostyLand.IntegrationTests.Infrastructure;

namespace PostyLand.IntegrationTests;

[Collection(IntegrationCollection.Name)]
public sealed class TenantManagementTests(PostyLandIntegrationFixture fixture)
{
    [Fact]
    public async Task TenantCrudEndpoints_ShouldCreateReadUpdateAndDeleteTenant()
    {
        var tenantId = Guid.NewGuid();
        var platformToken = CreateToken(Guid.NewGuid(), Guid.NewGuid(), RoleStatus.PlatformAdmin, "platform.admin");

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/tenants");
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        createRequest.Content = JsonContent.Create(new
        {
            id = tenantId,
            name = "Managed Tenant",
            subdomain = "managed-tenant",
            encryptedConnectionString = "encrypted-managed-connection",
            isActive = true,
            createdAt = new DateTime(2026, 3, 8, 0, 0, 0, DateTimeKind.Utc)
        });

        var createResponse = await fixture.Client.SendAsync(createRequest);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using (var createDoc = JsonDocument.Parse(createBody))
        {
            Assert.Equal(tenantId, createDoc.RootElement.GetProperty("id").GetGuid());
            Assert.Equal("Managed Tenant", createDoc.RootElement.GetProperty("name").GetString());
            Assert.Equal("managed-tenant", createDoc.RootElement.GetProperty("subdomain").GetString());
            Assert.Equal("encrypted-managed-connection", createDoc.RootElement.GetProperty("encryptedConnectionString").GetString());
            Assert.True(createDoc.RootElement.GetProperty("isActive").GetBoolean());
        }

        var listRequest = new HttpRequestMessage(HttpMethod.Get, "/api/tenants");
        listRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var listResponse = await fixture.Client.SendAsync(listRequest);
        var listBody = await listResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        using (var listDoc = JsonDocument.Parse(listBody))
        {
            Assert.Contains(listDoc.RootElement.EnumerateArray(), x => x.GetProperty("id").GetGuid() == tenantId);
        }

        var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/tenants/{tenantId}");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var getResponse = await fixture.Client.SendAsync(getRequest);
        var getBody = await getResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using (var getDoc = JsonDocument.Parse(getBody))
        {
            Assert.Equal(tenantId, getDoc.RootElement.GetProperty("id").GetGuid());
            Assert.Equal("Managed Tenant", getDoc.RootElement.GetProperty("name").GetString());
        }

        var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/tenants/{tenantId}");
        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        updateRequest.Content = JsonContent.Create(new
        {
            id = tenantId,
            name = "Managed Tenant Updated",
            subdomain = "managed-tenant-updated",
            encryptedConnectionString = "encrypted-managed-connection-updated",
            isActive = false,
            createdAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc)
        });

        var updateResponse = await fixture.Client.SendAsync(updateRequest);
        var updateBody = await updateResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using (var updateDoc = JsonDocument.Parse(updateBody))
        {
            Assert.Equal(tenantId, updateDoc.RootElement.GetProperty("id").GetGuid());
            Assert.Equal("Managed Tenant Updated", updateDoc.RootElement.GetProperty("name").GetString());
            Assert.Equal("managed-tenant-updated", updateDoc.RootElement.GetProperty("subdomain").GetString());
            Assert.Equal("encrypted-managed-connection-updated", updateDoc.RootElement.GetProperty("encryptedConnectionString").GetString());
            Assert.False(updateDoc.RootElement.GetProperty("isActive").GetBoolean());
        }

        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/tenants/{tenantId}");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var deleteResponse = await fixture.Client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getDeletedRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/tenants/{tenantId}");
        getDeletedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var getDeletedResponse = await fixture.Client.SendAsync(getDeletedRequest);
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    [Fact]
    public async Task TenantCrudEndpoints_ShouldRejectDuplicateSubdomain()
    {
        var platformToken = CreateToken(Guid.NewGuid(), Guid.NewGuid(), RoleStatus.PlatformAdmin, "platform.admin");
        var subdomain = $"duplicate-tenant-{Guid.NewGuid():N}"[..32];

        var firstCreate = new HttpRequestMessage(HttpMethod.Post, "/api/tenants");
        firstCreate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        firstCreate.Content = JsonContent.Create(new
        {
            name = "Duplicate One",
            subdomain,
            encryptedConnectionString = "duplicate-1",
            isActive = true
        });

        var firstResponse = await fixture.Client.SendAsync(firstCreate);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondCreate = new HttpRequestMessage(HttpMethod.Post, "/api/tenants");
        secondCreate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        secondCreate.Content = JsonContent.Create(new
        {
            name = "Duplicate Two",
            subdomain = subdomain.ToUpperInvariant(),
            encryptedConnectionString = "duplicate-2",
            isActive = true
        });

        var secondResponse = await fixture.Client.SendAsync(secondCreate);
        var secondBody = await secondResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
        Assert.Contains("already in use", secondBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TenantCrudAndMigrationEndpoints_ShouldRequirePlatformAdminPolicy()
    {
        var ownerToken = CreateToken(Guid.NewGuid(), Guid.NewGuid(), RoleStatus.Owner, "tenant.api");

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/tenants");
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        createRequest.Content = JsonContent.Create(new
        {
            name = "Denied Tenant",
            subdomain = "denied-tenant",
            encryptedConnectionString = "denied-connection",
            isActive = true
        });

        var createResponse = await fixture.Client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);

        var migrationRequest = new HttpRequestMessage(HttpMethod.Get, "/migrations/status");
        migrationRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var migrationResponse = await fixture.Client.SendAsync(migrationRequest);
        Assert.Equal(HttpStatusCode.Forbidden, migrationResponse.StatusCode);
    }

    [Fact]
    public async Task MigrationEndpoints_ShouldReportAndApplyMainAndTenantMigrations()
    {
        var tenant = await fixture.RegisterTenantAndWaitAsync();
        var platformToken = CreateToken(Guid.NewGuid(), Guid.NewGuid(), RoleStatus.PlatformAdmin, "platform.admin");

        var mainStatusRequest = new HttpRequestMessage(HttpMethod.Get, "/migrations/status");
        mainStatusRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var mainStatusResponse = await fixture.Client.SendAsync(mainStatusRequest);
        var mainStatusBody = await mainStatusResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, mainStatusResponse.StatusCode);

        using (var mainStatusDoc = JsonDocument.Parse(mainStatusBody))
        {
            Assert.True(mainStatusDoc.RootElement.GetProperty("mainDatabase").GetProperty("appliedMigrations").GetArrayLength() >= 1);
            Assert.Equal(0, mainStatusDoc.RootElement.GetProperty("mainDatabase").GetProperty("pendingMigrations").GetArrayLength());
        }

        var tenantStatusRequest = new HttpRequestMessage(HttpMethod.Get, $"/migrations/status?tenantId={tenant.TenantId}");
        tenantStatusRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var tenantStatusResponse = await fixture.Client.SendAsync(tenantStatusRequest);
        var tenantStatusBody = await tenantStatusResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, tenantStatusResponse.StatusCode);

        using (var tenantStatusDoc = JsonDocument.Parse(tenantStatusBody))
        {
            Assert.Equal(tenant.TenantId, tenantStatusDoc.RootElement.GetProperty("tenantDatabase").GetProperty("tenantId").GetGuid());
            Assert.True(tenantStatusDoc.RootElement.GetProperty("tenantDatabase").GetProperty("pendingMigrations").GetArrayLength() >= 1);
        }

        var applyRequest = new HttpRequestMessage(HttpMethod.Post, $"/migrations/apply?tenantId={tenant.TenantId}");
        applyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var applyResponse = await fixture.Client.SendAsync(applyRequest);
        var applyBody = await applyResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, applyResponse.StatusCode);

        using (var applyDoc = JsonDocument.Parse(applyBody))
        {
            Assert.Equal(tenant.TenantId, applyDoc.RootElement.GetProperty("tenantDatabase").GetProperty("tenantId").GetGuid());
            Assert.True(applyDoc.RootElement.GetProperty("tenantDatabase").GetProperty("appliedMigrations").GetArrayLength() >= 1);
            Assert.Equal(0, applyDoc.RootElement.GetProperty("tenantDatabase").GetProperty("pendingMigrations").GetArrayLength());
        }

        Assert.True(await HasMigrationHistoryAsync(tenant.TenantDatabase));
    }

    private async Task<bool> HasMigrationHistoryAsync(string databaseName)
    {
        await using var connection = new NpgsqlConnection(
            $"Host=127.0.0.1;Port=5432;Database={databaseName};Username=postgres;Password=postgres");
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "select 1 from information_schema.tables where table_schema='public' and table_name='__EFMigrationsHistory'";
        return await command.ExecuteScalarAsync() is not null;
    }

    private string CreateToken(Guid userId, Guid tenantId, RoleStatus role, string? scope)
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
