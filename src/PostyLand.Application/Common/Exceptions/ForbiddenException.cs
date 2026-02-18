namespace PostyLand.Application.Common.Exceptions;

public sealed class ForbiddenException(string message) : ApplicationExceptionBase(message);
