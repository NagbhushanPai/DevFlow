using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Projects.DTOs;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Mapster;

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
            .Select(p => p.Adapt<ProjectSummaryDto>())
            .ToListAsync(cancellationToken);
    }
}