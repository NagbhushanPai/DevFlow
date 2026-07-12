using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Features.Authentication.Common;
using MediatR;

namespace DevFlow.Application.Features.Authentication.Commands.Login;

public sealed class LoginCommandHandler
    : IRequestHandler<LoginCommand, AuthenticationResult>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IIdentityService identityService,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _identityService = identityService;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthenticationResult> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _identityService.GetUserByEmailAsync(
            request.Email,
            cancellationToken);

        if (user is null)
        {
            throw new InvalidCredentialsException();
        }

        var passwordIsValid =
            await _identityService.CheckPasswordAsync(
                user.Id,
                request.Password,
                cancellationToken);

        if (!passwordIsValid)
        {
            throw new InvalidCredentialsException();
        }

        var token = _jwtTokenGenerator.GenerateToken(user);

        return new AuthenticationResult(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Roles,
            token.Token,
            token.ExpiresAtUtc);
    }
}