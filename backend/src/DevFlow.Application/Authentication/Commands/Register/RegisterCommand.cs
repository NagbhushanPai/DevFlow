using DevFlow.Application.Authentication.Common;
using MediatR;

namespace DevFlow.Application.Authentication.Commands.Register;

public sealed record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password)
    : IRequest<AuthenticationResult>;
