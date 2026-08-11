using DevFlow.Application.Projects.DTOs;
using MediatR;

namespace DevFlow.Application.Projects.Queries.GetProjectById;

public sealed record GetProjectByIdQuery(Guid ProjectId)
    : IRequest<ProjectDto>;