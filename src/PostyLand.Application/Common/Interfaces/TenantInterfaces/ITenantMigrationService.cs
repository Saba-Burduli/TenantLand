namespace PostyLand.Application.Common.Interfaces.TenantInterfaces;

public interface ITenantMigrationService
{
    Task RunForTenantAsync(Guid tenantId, CancellationToken cancellationToken);
}

