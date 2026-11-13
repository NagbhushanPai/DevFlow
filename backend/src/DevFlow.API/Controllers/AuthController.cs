using DevFlow.Application.Authentication.Commands.Login;
using DevFlow.Application.Authentication.Commands.Refresh;
using DevFlow.Application.Authentication.Commands.Register;
using DevFlow.Application.Authentication.Common;
using DevFlow.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DevFlow.API.Controllers;

/// <summary>
/// Access tokens are returned in the body (the SPA keeps them in memory only); refresh tokens are set as an
/// HttpOnly, SameSite=Strict cookie scoped to /api/auth so scripts can never read them.
/// </summary>
[Route("api/auth")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
public sealed class AuthController(ISender sender, IRefreshTokenService refreshTokens) : BaseApiController
{
    public const string RefreshCookieName = "devflow_refresh";
    private const string RefreshCookiePath = "/api/auth";

    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RegisterCommand(request.FirstName, request.LastName, request.Email, request.Password), ct);
        return Ok(Respond(result));
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new LoginCommand(request.Email, request.Password), ct);
        return Ok(Respond(result));
    }

    /// <summary>Exchanges the refresh cookie for a new access token (and rotates the cookie).</summary>
    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken ct)
    {
        var token = Request.Cookies[RefreshCookieName];
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized();
        }

        try
        {
            var result = await sender.Send(new RefreshTokenCommand(token), ct);
            return Ok(Respond(result));
        }
        catch (Application.Common.Exceptions.InvalidCredentialsException)
        {
            ClearRefreshCookie();
            return Unauthorized();
        }
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var token = Request.Cookies[RefreshCookieName];
        if (!string.IsNullOrEmpty(token))
        {
            await refreshTokens.RevokeAsync(token, ct);
        }

        ClearRefreshCookie();
        return NoContent();
    }

    private AuthResponse Respond(AuthenticationResult result)
    {
        Response.Cookies.Append(RefreshCookieName, result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = result.RefreshTokenExpiresAtUtc,
            IsEssential = true,
        });

        return new AuthResponse(result.UserId, result.FirstName, result.LastName, result.Email, result.Roles, result.Token, result.ExpiresAtUtc);
    }

    private void ClearRefreshCookie() =>
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = RefreshCookiePath, Secure = Request.IsHttps, SameSite = SameSiteMode.Strict, HttpOnly = true });
}

public sealed record AuthResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyCollection<string> Roles,
    string Token,
    DateTime ExpiresAtUtc);
