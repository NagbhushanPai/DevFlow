namespace DevFlow.Application.Features.Authentication.Common;

public sealed record AuthenticationResult(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string Token,
    DateTime ExpiresAtUtc);