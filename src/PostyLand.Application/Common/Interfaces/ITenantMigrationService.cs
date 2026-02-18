namespace PostyLand.Application.Common.Interfaces;

public interface ITenantMigrationService
{
    Task RunForTenantAsync(Guid tenantId, CancellationToken cancellationToken);
}
