using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using DevFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DevFlow.Application.Common.Authorization;

/// <summary>
/// Resolves the current user's effective role in a project and enforces minimum roles.
/// </summary>
/// <remarks>
/// Role strength: <see cref="ProjectMemberRole.Manager"/> &gt; <see cref="ProjectMemberRole.Developer"/>
/// &gt; <see cref="ProjectMemberRole.Viewer"/>. Organization owners/admins are treated as project managers
/// for every project in their organization, even without an explicit project membership.
/// </remarks>
public interface IProjectAuthorizationService
{
    Guid CurrentUserId { get; }

    Task<ProjectMemberRole?> GetEffectiveRoleAsync(Guid projectId, CancellationToken ct);

    /// <summary>Same as above for an explicit user (e.g. a SignalR connection, where there is no HTTP request scope).</summary>
    Task<ProjectMemberRole?> GetEffectiveRoleAsync(Guid projectId, Guid userId, CancellationToken ct);

    /// <summary>Throws <see cref="ForbiddenAccessException"/> unless the user has at least <paramref name="minimum"/>.</summary>
    Task<ProjectMemberRole> RequireRoleAsync(Guid projectId, ProjectMemberRole minimum, CancellationToken ct);
}

public sealed class ProjectAuthorizationService(IApplicationDbContext context, ICurrentUserService currentUser)
    : IProjectAuthorizationService
{
    public Guid CurrentUserId => currentUser.UserId ?? throw new UnauthorizedAccessException();

    public Task<ProjectMemberRole?> GetEffectiveRoleAsync(Guid projectId, CancellationToken ct) =>
        GetEffectiveRoleAsync(projectId, CurrentUserId, ct);

    public async Task<ProjectMemberRole?> GetEffectiveRoleAsync(Guid projectId, Guid userId, CancellationToken ct)
    {
        var projectRole = await context.ProjectMembers
            .Where(member => member.ProjectId == projectId && member.UserId == userId)
            .Select(member => (ProjectMemberRole?)member.Role)
            .SingleOrDefaultAsync(ct);

        if (projectRole == ProjectMemberRole.Manager)
        {
            return projectRole;
        }

        var organizationRole = await context.OrganizationMembers
            .Where(member => member.UserId == userId
                && context.Projects.Any(project => project.Id == projectId && project.OrganizationId == member.OrganizationId))
            .Select(member => member.Role)
            .SingleOrDefaultAsync(ct);

        return organizationRole is not null && OrganizationRoles.CanManage(organizationRole)
            ? ProjectMemberRole.Manager
            : projectRole;
    }

    public async Task<ProjectMemberRole> RequireRoleAsync(Guid projectId, ProjectMemberRole minimum, CancellationToken ct)
    {
        var role = await GetEffectiveRoleAsync(projectId, ct)
            ?? throw new ForbiddenAccessException("Project membership is required.");

        // Lower enum value = more privileges (Manager = 1, Developer = 2, Viewer = 3).
        if (role > minimum)
        {
            throw new ForbiddenAccessException($"This action requires the {minimum} project role or higher.");
        }

        return role;
    }
}
