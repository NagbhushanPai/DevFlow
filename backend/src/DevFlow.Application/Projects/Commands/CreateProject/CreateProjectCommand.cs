using MediatR;

namespace DevFlow.Application.Projects.Commands.CreateProject;

public sealed record CreateProjectCommand(
    Guid OrganizationId,
    string Name,
    string Key,
    string? Description
) : IRequest<Guid>;
