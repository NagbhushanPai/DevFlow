using DevFlow.Application.Common.Interfaces;
using DevFlow.Domain.Entities;
using DevFlow.Domain.Enums;
using MediatR;

namespace DevFlow.Application.Projects.Commands.CreateProject;

public sealed class CreateProjectCommandHandler
    : IRequestHandler<CreateProjectCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateProjectCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(
        CreateProjectCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException();

        var project = new Project
        {
            Name = request.Name.Trim(),
            Key = request.Key.Trim().ToUpperInvariant(),
            Description = request.Description?.Trim(),
            Status = ProjectStatus.Active,
            OwnerId = userId
        };

        project.Members.Add(new ProjectMember
        {
            UserId = userId,
            Role = ProjectMemberRole.Manager,
            JoinedAtUtc = DateTime.UtcNow
        });

        _context.Projects.Add(project);

        await _context.SaveChangesAsync(cancellationToken);

        return project.Id;
    }
}