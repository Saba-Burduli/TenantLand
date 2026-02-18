using PostyLand.Application.Common.Contexts;

namespace PostyLand.Application.Common.Interfaces;

public interface IUserContextProvider
{
    UserContext? Current { get; }
    void Set(UserContext userContext);
}
