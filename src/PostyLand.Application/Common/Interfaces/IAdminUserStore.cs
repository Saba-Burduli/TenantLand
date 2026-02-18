using PostyLand.Domain.Entities;

namespace PostyLand.Application.Common.Interfaces;

public interface IAdminUserStore
{
    Task AddAsync(AdminUser adminUser, CancellationToken cancellationToken);
}
