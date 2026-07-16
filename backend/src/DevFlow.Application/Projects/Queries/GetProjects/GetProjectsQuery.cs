using DevFlow.Application.Projects.DTOs;
using MediatR;

namespace DevFlow.Application.Projects.Queries.GetProjects;

public sealed record GetProjectsQuery
    : IRequest<IReadOnlyList<ProjectSummaryDto>>;