using DevFlow.Application.Features.Authentication.Common;
using MediatR;

namespace DevFlow.Application.Features.Authentication.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password)
    : IRequest<AuthenticationResult>;