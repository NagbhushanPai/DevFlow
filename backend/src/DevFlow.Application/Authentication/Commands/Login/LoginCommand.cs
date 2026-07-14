using DevFlow.Application.Authentication.Common;
using MediatR;

namespace DevFlow.Application.Authentication.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password)
    : IRequest<AuthenticationResult>;
