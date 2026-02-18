namespace PostyLand.Application.Common.Exceptions;

public sealed class InfrastructureException(string message, Exception? innerException = null)
    : ApplicationExceptionBase(message, innerException);
