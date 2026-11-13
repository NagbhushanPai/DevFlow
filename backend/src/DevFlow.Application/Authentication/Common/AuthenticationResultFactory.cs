using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Common.Models;

namespace DevFlow.Application.Authentication.Common;

internal static class AuthenticationResultFactory
{
    public static AuthenticationResult Create(UserInfo user, JwtTokenResult accessToken, RefreshTokenResult refreshToken) =>
        new(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Roles,
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            refreshToken.Token,
            refreshToken.ExpiresAtUtc);
}
