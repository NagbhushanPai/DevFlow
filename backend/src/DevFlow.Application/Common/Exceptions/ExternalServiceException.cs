namespace DevFlow.Application.Common.Exceptions;

public sealed class ExternalServiceException(string message, Exception? innerException = null) : Exception(message, innerException);
