using DevFlow.Application.Features.Authentication.Common;
using MediatR;

namespace DevFlow.Application.Features.Authentication.Commands.Register;

public sealed record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password)
    : IRequest<AuthenticationResult>;