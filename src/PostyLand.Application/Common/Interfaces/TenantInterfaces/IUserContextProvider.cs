using PostyLand.Application.Common.Contexts;

namespace PostyLand.Application.Common.Interfaces.TenantInterfaces;

public interface IUserContextProvider
{
    UserContext? Current { get; }
    void Set(UserContext userContext);
}

