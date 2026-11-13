using DevFlow.Application.Authentication.Common;
using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using MediatR;

namespace DevFlow.Application.Authentication.Commands.Login;

public sealed class LoginCommandHandler(
    IIdentityService identityService,
    IJwtTokenGenerator jwtTokenGenerator,
    IRefreshTokenService refreshTokenService)
    : IRequestHandler<LoginCommand, AuthenticationResult>
{
    public async Task<AuthenticationResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await identityService.GetUserByEmailAsync(request.Email, cancellationToken)
            ?? throw new InvalidCredentialsException();

        if (!await identityService.CheckPasswordAsync(user.Id, request.Password, cancellationToken))
        {
            throw new InvalidCredentialsException();
        }

        var accessToken = jwtTokenGenerator.GenerateToken(user);
        var refreshToken = await refreshTokenService.IssueAsync(user.Id, cancellationToken);
        return AuthenticationResultFactory.Create(user, accessToken, refreshToken);
    }
}
