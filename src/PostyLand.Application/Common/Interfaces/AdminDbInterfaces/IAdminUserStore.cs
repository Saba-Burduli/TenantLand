using PostyLand.Domain.Entities;

namespace PostyLand.Application.Common.Interfaces.AdminDbInterfaces;

public interface IAdminUserStore
{
    Task AddAsync(AdminUser adminUser, CancellationToken cancellationToken);
}

