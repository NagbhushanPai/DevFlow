using DevFlow.Application.Authentication.Common;
using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using MediatR;

namespace DevFlow.Application.Authentication.Commands.Refresh;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthenticationResult>;

public sealed class RefreshTokenCommandHandler(
    IIdentityService identityService,
    IJwtTokenGenerator jwtTokenGenerator,
    IRefreshTokenService refreshTokenService)
    : IRequestHandler<RefreshTokenCommand, AuthenticationResult>
{
    public async Task<AuthenticationResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new InvalidCredentialsException();
        }

        var rotation = await refreshTokenService.RotateAsync(request.RefreshToken, cancellationToken)
            ?? throw new InvalidCredentialsException();

        var user = await identityService.GetUserByIdAsync(rotation.UserId, cancellationToken)
            ?? throw new InvalidCredentialsException();

        var accessToken = jwtTokenGenerator.GenerateToken(user);
        return AuthenticationResultFactory.Create(user, accessToken, rotation.NewToken);
    }
}
