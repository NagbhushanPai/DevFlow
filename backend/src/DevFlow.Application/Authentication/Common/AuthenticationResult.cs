namespace DevFlow.Application.Authentication.Common;

public sealed record AuthenticationResult(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyCollection<string> Roles,
    string Token,
    DateTime ExpiresAtUtc);
