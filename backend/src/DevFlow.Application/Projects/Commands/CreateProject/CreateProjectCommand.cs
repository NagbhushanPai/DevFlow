using MediatR;

namespace DevFlow.Application.Projects.Commands.CreateProject;

public sealed record CreateProjectCommand(
    string Name,
    string Key,
    string? Description
) : IRequest<Guid>;