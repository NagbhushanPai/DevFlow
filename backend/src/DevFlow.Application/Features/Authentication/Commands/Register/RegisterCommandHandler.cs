using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Common.Models;
using DevFlow.Application.Features.Authentication.Common;
using MediatR;

namespace DevFlow.Application.Features.Authentication.Commands.Register;

public sealed class RegisterCommandHandler
    : IRequestHandler<RegisterCommand, AuthenticationResult>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterCommandHandler(
        IIdentityService identityService,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _identityService = identityService;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthenticationResult> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var userExists =
            await _identityService.UserExistsByEmailAsync(
                request.Email,
                cancellationToken);

        if (userExists)
        {
            throw new RegistrationException(
                ["A user with this email already exists."]);
        }

        var creationResult =
            await _identityService.CreateUserAsync(
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password,
                cancellationToken);

        if (!creationResult.Succeeded ||
            creationResult.UserId is null)
        {
            throw new RegistrationException(
                creationResult.Errors);
        }

        var user = new UserInfo(
            creationResult.UserId.Value,
            request.FirstName,
            request.LastName,
            request.Email);

        var token = _jwtTokenGenerator.GenerateToken(user);

        return new AuthenticationResult(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            token.Token,
            token.ExpiresAtUtc);
    }
}