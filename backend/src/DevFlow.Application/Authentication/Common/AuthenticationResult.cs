namespace DevFlow.Application.Authentication.Common;

/// <summary>
/// Result of a successful login/register/refresh. The API returns everything except the refresh token in the
/// response body; the refresh token is written to an HttpOnly cookie.
/// </summary>
public sealed record AuthenticationResult(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyCollection<string> Roles,
    string Token,
    DateTime ExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
