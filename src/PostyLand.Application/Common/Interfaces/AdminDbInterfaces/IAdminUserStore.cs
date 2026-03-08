using PostyLand.Domain.Entities;
using PostyLand.Domain.Enums;

namespace PostyLand.Application.Common.Interfaces.AdminDbInterfaces;

public interface IAdminUserStore
{
    Task AddAsync(AdminUser adminUser, CancellationToken cancellationToken);
    Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<AdminUser?> GetByExternalProviderAsync(
        ExternalAuthProvider provider,
        string externalProviderId,
        CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

