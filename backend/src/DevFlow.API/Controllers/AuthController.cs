using DevFlow.Application.Features.Authentication.Commands.Login;
using DevFlow.Application.Features.Authentication.Commands.Register;
using DevFlow.Application.Features.Authentication.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevFlow.API.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    [ProducesResponseType(
        typeof(AuthenticationResult),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthenticationResult>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password);

        var result = await _sender.Send(
            command,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("login")]
    [ProducesResponseType(
        typeof(AuthenticationResult),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthenticationResult>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(
            request.Email,
            request.Password);

        var result = await _sender.Send(
            command,
            cancellationToken);

        return Ok(result);
    }
}