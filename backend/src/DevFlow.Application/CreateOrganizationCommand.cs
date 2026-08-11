using MediatR;

namespace DevFlow.Application.Organizations.Commands.CreateOrganization;

public sealed record CreateOrganizationCommand(
    string Name,
    string? Description) : IRequest<Guid>;
