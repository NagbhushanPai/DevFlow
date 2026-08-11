using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Projects.DTOs;
using DevFlow.Domain.Enums;
using Mapster;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace DevFlow.Application.Projects.Queries.GetProjectById;

public sealed class GetProjectByIdQueryHandler
    : IRequestHandler<GetProjectByIdQuery, ProjectDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetProjectByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ProjectDto> Handle(
        GetProjectByIdQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException();

        var project = await _context.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(
                p => p.Id == request.ProjectId,
                cancellationToken);

        if (project is null)
            throw new KeyNotFoundException("Project not found.");

        var isMember = project.Members.Any(
            m => m.UserId == userId);

        if (!isMember)
            throw new UnauthorizedAccessException();

        return project.Adapt<ProjectDto>();
    }
}