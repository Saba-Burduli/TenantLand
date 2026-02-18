using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PostyLand.Domain.Enums;
using PostyLand.Persistence.Context;

namespace PostyLand.IntegrationTests.Infrastructure;

public sealed class PostyLandIntegrationFixture : IAsyncLifetime
{
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];
    private int _tenantCounter;
    private readonly HashSet<string> _createdTenantDatabases = [];

    public PostyLandApiFactory Factory { get; private set; } = default!;
    public HttpClient Client { get; private set; } = default!;

    public string MainDbName => $"postyland_main_it_{_suffix}";
    public string MainDbConnectionString => $"Host=127.0.0.1;Port=5432;Database={MainDbName};Username=postgres;Password=postgres";
    public string PostgresAdminConnectionString => "Host=127.0.0.1;Port=5432;Database=postgres;Username=postgres;Password=postgres";
    public string JwtSigningKey => "replace-with-at-least-32-characters-long-development-key";
    public string JwtIssuer => "postyland-api-dev";
    public string JwtAudience => "postyland-clients-dev";

    public async Task InitializeAsync()
    {
        await EnsureDockerComposeUpAsync();
        await WaitForPostgresReadyAsync();
        await CreateDatabaseIfNotExistsAsync(MainDbName);

        var overrides = new Dictionary<string, string?>
        {
            ["ConnectionStrings:MainDb"] = MainDbConnectionString,
            ["TenantResolution:RootDomain"] = "postyland.com",
            ["TenantResolution:SubdomainHeader"] = "X-Tenant-Subdomain",
            ["Encryption:Key"] = $"postyland-it-encryption-key-{_suffix}",
            ["TenantDatabase:Template"] = "Host=127.0.0.1;Port=5432;Username=postgres;Password=postgres;Database=tenant_template_it",
            ["Provisioning:PostgresAdminConnectionString"] = PostgresAdminConnectionString,
            ["Provisioning:DisableExternalProvisioning"] = "true",
            ["Provisioning:BucketPrefix"] = $"postyland-it-{_suffix}"
        };

        Factory = new PostyLandApiFactory(overrides);
        Client = Factory.CreateClient();

        await using var scope = Factory.Services.CreateAsyncScope();
        var mainDb = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        await mainDb.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();

        foreach (var tenantDb in _createdTenantDatabases)
        {
            await DropDatabaseIfExistsAsync(tenantDb);
        }

        await DropDatabaseIfExistsAsync(MainDbName);
    }

    public async Task<RegisteredTenant> RegisterTenantAndWaitAsync(string? subdomain = null)
    {
        subdomain ??= $"it{_suffix}{Interlocked.Increment(ref _tenantCounter)}";
        var request = new
        {
            name = $"Tenant {subdomain}",
            subdomain,
            adminEmail = $"owner-{subdomain}@example.com",
            adminPassword = "StrongPass123!",
            plan = "Pro",
            employeeLimit = 25,
            renewalDate = DateTime.UtcNow.AddYears(2)
        };

        var response = await Client.PostAsJsonAsync("/api/tenants/register", request);
        var payload = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(payload);
        var tenantId = doc.RootElement.GetProperty("tenantId").GetGuid();
        var jobId = doc.RootElement.GetProperty("onboardingJobId").GetString() ?? string.Empty;
        _createdTenantDatabases.Add($"tenant_{subdomain}");

        await WaitForTenantStatusAsync(tenantId, TenantOnboardingStatus.Completed, TimeSpan.FromSeconds(45));

        return new RegisteredTenant
        {
            TenantId = tenantId,
            Subdomain = subdomain,
            OnboardingJobId = jobId,
            TenantDatabase = $"tenant_{subdomain}"
        };
    }

    public async Task WaitForTenantStatusAsync(Guid tenantId, TenantOnboardingStatus status, TimeSpan timeout)
    {
        var startedAt = DateTime.UtcNow;
        while (DateTime.UtcNow - startedAt < timeout)
        {
            await using var scope = Factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var tenant = await db.Tenants.AsNoTracking().SingleOrDefaultAsync(x => x.Id == tenantId);
            if (tenant is not null && tenant.Status == status)
            {
                return;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException($"Tenant '{tenantId}' did not reach status '{status}' in time.");
    }

    public async Task<bool> DatabaseExistsAsync(string databaseName)
    {
        await using var connection = new NpgsqlConnection(PostgresAdminConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "select 1 from pg_database where datname = @name";
        command.Parameters.AddWithValue("@name", databaseName);
        return await command.ExecuteScalarAsync() is not null;
    }

    public async Task<string?> GetHangfireJobStateAsync(string jobId)
    {
        using var scope = Factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<JobStorage>();
        var details = storage.GetMonitoringApi().JobDetails(jobId);
        return details?.History.FirstOrDefault()?.StateName;
    }

    public string GetRuntimeConfigurationValue(string key)
    {
        using var scope = Factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        return configuration[key] ?? string.Empty;
    }

    private async Task EnsureDockerComposeUpAsync()
    {
        var root = GetSolutionRoot();
        await RunProcessAsync("docker", "compose up -d", root);
    }

    private async Task WaitForPostgresReadyAsync()
    {
        var timeout = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < timeout)
        {
            try
            {
                await using var connection = new NpgsqlConnection(PostgresAdminConnectionString);
                await connection.OpenAsync();
                return;
            }
            catch
            {
                await Task.Delay(1000);
            }
        }

        throw new TimeoutException("PostgreSQL did not become ready in time.");
    }

    private async Task CreateDatabaseIfNotExistsAsync(string databaseName)
    {
        await using var connection = new NpgsqlConnection(PostgresAdminConnectionString);
        await connection.OpenAsync();

        await using var existsCommand = connection.CreateCommand();
        existsCommand.CommandText = "select 1 from pg_database where datname = @name";
        existsCommand.Parameters.AddWithValue("@name", databaseName);
        var exists = await existsCommand.ExecuteScalarAsync() is not null;
        if (exists)
        {
            return;
        }

        await using var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"create database \"{databaseName}\"";
        await createCommand.ExecuteNonQueryAsync();
    }

    private async Task DropDatabaseIfExistsAsync(string databaseName)
    {
        await using var connection = new NpgsqlConnection(PostgresAdminConnectionString);
        await connection.OpenAsync();

        await using var terminateCommand = connection.CreateCommand();
        terminateCommand.CommandText = @"
            select pg_terminate_backend(pid)
            from pg_stat_activity
            where datname = @name and pid <> pg_backend_pid();";
        terminateCommand.Parameters.AddWithValue("@name", databaseName);
        await terminateCommand.ExecuteNonQueryAsync();

        await using var dropCommand = connection.CreateCommand();
        dropCommand.CommandText = $"drop database if exists \"{databaseName}\"";
        await dropCommand.ExecuteNonQueryAsync();
    }

    private static string GetSolutionRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }

    private static async Task RunProcessAsync(string fileName, string arguments, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workingDirectory
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start '{fileName}'.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Command '{fileName} {arguments}' failed.{Environment.NewLine}STDOUT:{Environment.NewLine}{output}{Environment.NewLine}STDERR:{Environment.NewLine}{error}");
        }
    }
}

public sealed class RegisteredTenant
{
    public Guid TenantId { get; init; }
    public string Subdomain { get; init; } = string.Empty;
    public string OnboardingJobId { get; init; } = string.Empty;
    public string TenantDatabase { get; init; } = string.Empty;
}
