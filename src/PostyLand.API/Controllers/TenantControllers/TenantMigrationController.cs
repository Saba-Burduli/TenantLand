using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostyLand.Application.Common.Exceptions;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;
using PostyLand.Persistence.Context;

namespace PostyLand.API.Controllers;

[Route("migrations")]
public sealed class TenantMigrationController(
    MainDbContext mainDbContext,
    ITenantConnectionResolver tenantConnectionResolver) : AdminBaseController
{
    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromQuery] Guid? tenantId, CancellationToken cancellationToken)
    {
        await mainDbContext.Database.MigrateAsync(cancellationToken);
        var mainDatabase = await GetMigrationStatusAsync(mainDbContext, cancellationToken);

        if (!tenantId.HasValue)
        {
            return Ok(new { mainDatabase });
        }

        await using var tenantDbContext = await CreateTenantDbContextAsync(tenantId.Value, cancellationToken);
        await tenantDbContext.Database.MigrateAsync(cancellationToken);
        var tenantDatabase = await GetMigrationStatusAsync(tenantDbContext, cancellationToken);

        return Ok(new
        {
            mainDatabase,
            tenantDatabase = new TenantDatabaseMigrationStatus(
                tenantId.Value,
                tenantDatabase.AppliedMigrations,
                tenantDatabase.PendingMigrations)
        });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus([FromQuery] Guid? tenantId, CancellationToken cancellationToken)
    {
        var mainDatabase = await GetMigrationStatusAsync(mainDbContext, cancellationToken);

        if (!tenantId.HasValue)
        {
            return Ok(new { mainDatabase });
        }

        await using var tenantDbContext = await CreateTenantDbContextAsync(tenantId.Value, cancellationToken);
        var tenantDatabase = await GetMigrationStatusAsync(tenantDbContext, cancellationToken);

        return Ok(new
        {
            mainDatabase,
            tenantDatabase = new TenantDatabaseMigrationStatus(
                tenantId.Value,
                tenantDatabase.AppliedMigrations,
                tenantDatabase.PendingMigrations)
        });
    }

    private async Task<TenantDbContext> CreateTenantDbContextAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await mainDbContext.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tenantId, cancellationToken);

        if (tenant is null)
        {
            throw new NotFoundException($"Tenant '{tenantId}' does not exist.");
        }

        var decryptedConnectionString =
            tenantConnectionResolver.ResolveDecryptedConnectionString(tenant.EncryptedConnectionString);

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(
                decryptedConnectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(TenantDbContext).Assembly.FullName))
            .Options;

        return new TenantDbContext(options);
    }

    private static async Task<MigrationStatus> GetMigrationStatusAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
        return new MigrationStatus(appliedMigrations, pendingMigrations);
    }

    private sealed record MigrationStatus(string[] AppliedMigrations, string[] PendingMigrations);

    private sealed record TenantDatabaseMigrationStatus(
        Guid TenantId,
        string[] AppliedMigrations,
        string[] PendingMigrations);
}
