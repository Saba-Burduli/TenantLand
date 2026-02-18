namespace PostyLand.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string Hash(string input);
}
