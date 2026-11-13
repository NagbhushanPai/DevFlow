namespace DevFlow.Application.Common.Exceptions;

/// <summary>
/// The request conflicts with the current state of a resource (HTTP 409).
/// </summary>
public sealed class ConflictException(string message) : Exception(message);
