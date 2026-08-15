using DevFlow.Application.Common.Interfaces;
using DevFlow.Domain.Entities;
using DevFlow.Domain.Enums;
using DevFlow.Application.Common.Authorization;
using MediatR;

namespace DevFlow.Application.Projects.Commands.CreateProject;

public sealed class CreateProjectCommandHandler
    : IRequestHandler<CreateProjectCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOrganizationAuthorizationService _organizationAuthorizationService;

    public CreateProjectCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IOrganizationAuthorizationService organizationAuthorizationService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _organizationAuthorizationService = organizationAuthorizationService;
    }

    public async Task<Guid> Handle(
        CreateProjectCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException();

        await _organizationAuthorizationService.RequireManagerAsync(
            request.OrganizationId, userId, cancellationToken);

        var project = new Project
        {
            OrganizationId = request.OrganizationId,
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
