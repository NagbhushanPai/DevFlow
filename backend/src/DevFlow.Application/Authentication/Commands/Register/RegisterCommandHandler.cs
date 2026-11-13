using DevFlow.Application.Authentication.Common;
using DevFlow.Application.Common.Authorization;
using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Common.Models;
using MediatR;

namespace DevFlow.Application.Authentication.Commands.Register;

public sealed class RegisterCommandHandler(
    IIdentityService identityService,
    IJwtTokenGenerator jwtTokenGenerator,
    IRefreshTokenService refreshTokenService)
    : IRequestHandler<RegisterCommand, AuthenticationResult>
{
    public async Task<AuthenticationResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await identityService.UserExistsByEmailAsync(request.Email, cancellationToken))
        {
            throw new RegistrationException(["A user with this email already exists."]);
        }

        var creationResult = await identityService.CreateUserAsync(
            request.FirstName.Trim(),
            request.LastName.Trim(),
            request.Email.Trim(),
            request.Password,
            cancellationToken);

        if (!creationResult.Succeeded || creationResult.UserId is null)
        {
            throw new RegistrationException(creationResult.Errors);
        }

        await identityService.AddToRoleAsync(creationResult.UserId.Value, Roles.Developer, cancellationToken);

        var user = new UserInfo(
            creationResult.UserId.Value,
            request.FirstName.Trim(),
            request.LastName.Trim(),
            request.Email.Trim(),
            [Roles.Developer]);

        var accessToken = jwtTokenGenerator.GenerateToken(user);
        var refreshToken = await refreshTokenService.IssueAsync(user.Id, cancellationToken);
        return AuthenticationResultFactory.Create(user, accessToken, refreshToken);
    }
}
