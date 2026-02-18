using PostyLand.Application.Common.Contexts;
using PostyLand.Application.Common.Interfaces;

namespace PostyLand.Infrastructure.MultiTenancy;

public sealed class UserContextProvider : IUserContextProvider
{
    public UserContext? Current { get; private set; }

    public void Set(UserContext userContext)
    {
        Current = userContext;
    }
}
