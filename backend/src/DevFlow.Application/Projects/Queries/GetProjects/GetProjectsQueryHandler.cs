using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Projects.DTOs;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace DevFlow.Application.Projects.Queries.GetProjects;

public sealed class GetProjectsQueryHandler
    : IRequestHandler<GetProjectsQuery, IReadOnlyList<ProjectSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetProjectsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ProjectSummaryDto>> Handle(
        GetProjectsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException();

        return await _context.ProjectMembers
            .Where(pm => pm.UserId == userId)
            .Select(pm => pm.Project)
            .OrderBy(p => p.Name)
            .Select(p => new ProjectSummaryDto(
                p.Id,
                p.Name,
                p.Key,
                p.Description,
                p.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}