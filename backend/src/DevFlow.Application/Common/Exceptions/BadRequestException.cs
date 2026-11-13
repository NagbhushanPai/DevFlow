namespace DevFlow.Application.Common.Exceptions;

/// <summary>
/// Invalid client input detected by application logic (HTTP 400). Use this rather than a bare
/// <see cref="ArgumentException"/> so that unexpected framework argument errors are reported as 500s and their
/// internal messages are never sent to clients.
/// </summary>
public sealed class BadRequestException(string message) : ArgumentException(message);
