using System.Text.RegularExpressions;
using DevFlow.Application.Common;
using DevFlow.Application.Common.Authorization;
using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Organizations.DTOs;
using DevFlow.Application.Projects.DTOs;
using DevFlow.Domain.Entities;
using DevFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DevFlow.Application.Projects;

public interface IProjectManagementService
{
    Task<Guid> CreateProjectAsync(CreateProjectInput input, CancellationToken ct);
    Task<ProjectPage> GetProjectsAsync(ProjectQuery query, CancellationToken ct);
    Task<ProjectDto> GetProjectAsync(Guid projectId, CancellationToken ct);
    Task UpdateProjectAsync(Guid projectId, UpdateProjectInput input, CancellationToken ct);
    Task DeleteProjectAsync(Guid projectId, CancellationToken ct);
    Task<IReadOnlyList<ProjectMemberDto>> GetMembersAsync(Guid projectId, CancellationToken ct);
    Task SetMemberAsync(Guid projectId, Guid userId, ProjectMemberRole role, CancellationToken ct);
    Task RemoveMemberAsync(Guid projectId, Guid userId, CancellationToken ct);
    Task<IReadOnlyList<TeamDto>> GetTeamsAsync(Guid projectId, CancellationToken ct);
    Task AssignTeamAsync(Guid projectId, Guid teamId, CancellationToken ct);
    Task RemoveTeamAsync(Guid projectId, Guid teamId, CancellationToken ct);
    Task SetGitHubRepositoryAsync(Guid projectId, string? owner, string? name, CancellationToken ct);
    Task<GitHubRepositorySummary?> GetGitHubSummaryAsync(Guid projectId, CancellationToken ct);
}

/// <summary>
/// Permissions: members read; Managers (or organization owners/admins) change settings, members and teams.
/// Creating a project requires organization owner/admin.
/// </summary>
public sealed partial class ProjectManagementService(
    IApplicationDbContext context,
    IProjectAuthorizationService authorization,
    IOrganizationAuthorizationService organizationAuthorization,
    IIdentityService identityService,
    IGitHubIntegrationService gitHub,
    ICacheService cache) : IProjectManagementService
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(2);

    private Guid UserId => authorization.CurrentUserId;

    public async Task<Guid> CreateProjectAsync(CreateProjectInput input, CancellationToken ct)
    {
        await organizationAuthorization.RequireManagerAsync(input.OrganizationId, UserId, ct);

        var key = NormalizeKey(input.Key);
        if (await context.Projects.AnyAsync(project => project.Key == key, ct))
        {
            throw new ConflictException($"A project with key '{key}' already exists.");
        }

        var project = new Project
        {
            OrganizationId = input.OrganizationId,
            Name = Guard.Required(input.Name, "Project name", NameMaxLength),
            Key = key,
            Description = Guard.Optional(input.Description, "Description", DescriptionMaxLength),
            Status = ProjectStatus.Active,
            OwnerId = UserId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        project.Members.Add(new ProjectMember
        {
            UserId = UserId,
            Role = ProjectMemberRole.Manager,
            JoinedAtUtc = DateTime.UtcNow,
        });

        context.Projects.Add(project);
        await context.SaveChangesAsync(ct);
        return project.Id;
    }

    public async Task<ProjectPage> GetProjectsAsync(ProjectQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var userId = UserId;

        var managedOrganizationIds = context.OrganizationMembers
            .Where(member => member.UserId == userId
                && (member.Role == OrganizationRoles.Owner || member.Role == OrganizationRoles.Admin))
            .Select(member => member.OrganizationId);

        var query = context.Projects.Where(project =>
            project.Members.Any(member => member.UserId == userId)
            || managedOrganizationIds.Contains(project.OrganizationId));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(project => project.Name.Contains(term) || project.Key.Contains(term));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(project => project.Status == request.Status);
        }

        query = (request.SortBy?.ToLowerInvariant(), request.Descending) switch
        {
            ("key", false) => query.OrderBy(project => project.Key),
            ("key", true) => query.OrderByDescending(project => project.Key),
            ("createdat", false) => query.OrderBy(project => project.CreatedAtUtc),
            ("createdat", true) => query.OrderByDescending(project => project.CreatedAtUtc),
            (_, true) => query.OrderByDescending(project => project.Name),
            _ => query.OrderBy(project => project.Name),
        };

        var total = await query.CountAsync(ct);
        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(project => new
            {
                Project = project,
                MemberRole = project.Members.Where(member => member.UserId == userId).Select(member => (ProjectMemberRole?)member.Role).FirstOrDefault(),
                IsOrganizationManager = managedOrganizationIds.Contains(project.OrganizationId),
            })
            .ToListAsync(ct);

        var items = rows
            .Select(row => ToDto(row.Project) with
            {
                MyRole = row.IsOrganizationManager ? ProjectMemberRole.Manager : row.MemberRole,
            })
            .ToList();

        return new ProjectPage(items, page, pageSize, total);
    }

    public async Task<ProjectDto> GetProjectAsync(Guid projectId, CancellationToken ct)
    {
        var role = await authorization.RequireRoleAsync(projectId, ProjectMemberRole.Viewer, ct);

        // Cached per project (not per user) so that any update can invalidate it for everyone.
        var cacheKey = ProjectCacheKey(projectId);
        var dto = await cache.GetAsync<ProjectDto>(cacheKey, ct);
        if (dto is null)
        {
            dto = ToDto(await FindAsync(projectId, ct));
            await cache.SetAsync(cacheKey, dto, CacheLifetime, ct);
        }

        return dto with { MyRole = role };
    }

    public async Task UpdateProjectAsync(Guid projectId, UpdateProjectInput input, CancellationToken ct)
    {
        await authorization.RequireRoleAsync(projectId, ProjectMemberRole.Manager, ct);
        var project = await FindAsync(projectId, ct);

        var key = NormalizeKey(input.Key);
        if (key != project.Key && await context.Projects.AnyAsync(other => other.Key == key && other.Id != projectId, ct))
        {
            throw new ConflictException($"A project with key '{key}' already exists.");
        }

        project.Name = Guard.Required(input.Name, "Project name", NameMaxLength);
        project.Key = key;
        project.Description = Guard.Optional(input.Description, "Description", DescriptionMaxLength);
        project.Status = input.Status;
        project.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        await cache.RemoveAsync(ProjectCacheKey(projectId), ct);
    }

    public async Task DeleteProjectAsync(Guid projectId, CancellationToken ct)
    {
        await authorization.RequireRoleAsync(projectId, ProjectMemberRole.Manager, ct);
        var project = await FindAsync(projectId, ct);

        context.Projects.Remove(project);
        await context.SaveChangesAsync(ct);
        await cache.RemoveAsync(ProjectCacheKey(projectId), ct);
    }

    public async Task<IReadOnlyList<ProjectMemberDto>> GetMembersAsync(Guid projectId, CancellationToken ct)
    {
        await authorization.RequireRoleAsync(projectId, ProjectMemberRole.Viewer, ct);

        var members = await context.ProjectMembers
            .Where(member => member.ProjectId == projectId)
            .OrderBy(member => member.Role)
            .Select(member => new { member.UserId, member.Role })
            .ToListAsync(ct);

        var users = await identityService.GetUsersAsync(members.Select(member => member.UserId), ct);
        return members
            .Select(member =>
            {
                var user = users.GetValueOrDefault(member.UserId);
                return new ProjectMemberDto(member.UserId, member.Role, user?.FullName ?? "Unknown user", user?.Email ?? string.Empty);
            })
            .ToList();
    }

    public async Task SetMemberAsync(Guid projectId, Guid userId, ProjectMemberRole role, CancellationToken ct)
    {
        if (!Enum.IsDefined(role))
        {
            throw new BadRequestException("Role must be Manager, Developer or Viewer.");
        }

        await authorization.RequireRoleAsync(projectId, ProjectMemberRole.Manager, ct);
        var project = await FindAsync(projectId, ct);

        if (!await context.OrganizationMembers.AnyAsync(m => m.OrganizationId == project.OrganizationId && m.UserId == userId, ct))
        {
            throw new BadRequestException("A project member must belong to the organization.");
        }

        if (userId == project.OwnerId && role != ProjectMemberRole.Manager)
        {
            throw new BadRequestException("The project owner must remain a manager.");
        }

        var member = await context.ProjectMembers.SingleOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId, ct);
        if (member is null)
        {
            context.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = userId, Role = role, JoinedAtUtc = DateTime.UtcNow });
        }
        else
        {
            member.Role = role;
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task RemoveMemberAsync(Guid projectId, Guid userId, CancellationToken ct)
    {
        await authorization.RequireRoleAsync(projectId, ProjectMemberRole.Manager, ct);
        var project = await FindAsync(projectId, ct);

        var member = await context.ProjectMembers.SingleOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId, ct)
            ?? throw new NotFoundException("Project member was not found.");
        if (member.UserId == project.OwnerId)
        {
            throw new BadRequestException("The project owner cannot be removed.");
        }

        context.ProjectMembers.Remove(member);
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<TeamDto>> GetTeamsAsync(Guid projectId, CancellationToken ct)
    {
        await authorization.RequireRoleAsync(projectId, ProjectMemberRole.Viewer, ct);

        return await context.ProjectTeams
            .Where(item => item.ProjectId == projectId)
            .Select(item => new TeamDto(item.Team.Id, item.Team.OrganizationId, item.Team.Name, item.Team.Description))
            .ToListAsync(ct);
    }

    public async Task AssignTeamAsync(Guid projectId, Guid teamId, CancellationToken ct)
    {
        await authorization.RequireRoleAsync(projectId, ProjectMemberRole.Manager, ct);
        var project = await FindAsync(projectId, ct);

        var team = await context.Teams.SingleOrDefaultAsync(t => t.Id == teamId, ct)
            ?? throw new NotFoundException("Team was not found.");
        if (team.OrganizationId != project.OrganizationId)
        {
            throw new BadRequestException("A team must belong to the project's organization.");
        }

        if (!await context.ProjectTeams.AnyAsync(item => item.ProjectId == projectId && item.TeamId == teamId, ct))
        {
            context.ProjectTeams.Add(new ProjectTeam { ProjectId = projectId, TeamId = teamId, AssignedAtUtc = DateTime.UtcNow });
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task RemoveTeamAsync(Guid projectId, Guid teamId, CancellationToken ct)
    {
        await authorization.RequireRoleAsync(projectId, ProjectMemberRole.Manager, ct);

        var item = await context.ProjectTeams.SingleOrDefaultAsync(x => x.ProjectId == projectId && x.TeamId == teamId, ct)
            ?? throw new NotFoundException("Project team assignment was not found.");
        context.ProjectTeams.Remove(item);
        await context.SaveChangesAsync(ct);
    }

    public async Task SetGitHubRepositoryAsync(Guid projectId, string? owner, string? name, CancellationToken ct)
    {
        await authorization.RequireRoleAsync(projectId, ProjectMemberRole.Manager, ct);
        var project = await FindAsync(projectId, ct);

        var repositoryOwner = Guard.Optional(owner, "Repository owner", 100);
        var repositoryName = Guard.Optional(name, "Repository name", 100);
        if ((repositoryOwner is null) != (repositoryName is null))
        {
            throw new BadRequestException("Provide both the repository owner and name, or neither to unlink.");
        }

        if (repositoryOwner is not null && (!GitHubNamePattern().IsMatch(repositoryOwner) || !GitHubNamePattern().IsMatch(repositoryName!)))
        {
            throw new BadRequestException("Repository owner and name may only contain letters, digits, '-', '_' and '.'.");
        }

        project.GitHubRepositoryOwner = repositoryOwner;
        project.GitHubRepositoryName = repositoryName;
        project.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        await cache.RemoveAsync(ProjectCacheKey(projectId), ct);
    }

    public async Task<GitHubRepositorySummary?> GetGitHubSummaryAsync(Guid projectId, CancellationToken ct)
    {
        await authorization.RequireRoleAsync(projectId, ProjectMemberRole.Viewer, ct);
        var project = await FindAsync(projectId, ct);

        if (project.GitHubRepositoryOwner is null || project.GitHubRepositoryName is null)
        {
            return null;
        }

        return await gitHub.GetRepositorySummaryAsync(project.GitHubRepositoryOwner, project.GitHubRepositoryName, ct);
    }

    public static string NormalizeKey(string? key)
    {
        var normalized = Guard.Required(key, "Project key").ToUpperInvariant();
        if (!ProjectKeyPattern().IsMatch(normalized))
        {
            throw new BadRequestException("Project key must be 2-10 letters or digits.");
        }

        return normalized;
    }

    private static string ProjectCacheKey(Guid projectId) => $"project:{projectId:D}";

    private async Task<Project> FindAsync(Guid id, CancellationToken ct) =>
        await context.Projects.SingleOrDefaultAsync(project => project.Id == id, ct)
        ?? throw new NotFoundException("Project was not found.");

    private static ProjectDto ToDto(Project p) =>
        new(p.Id, p.OrganizationId, p.Name, p.Key, p.Description, p.Status, p.OwnerId, p.CreatedAtUtc, p.UpdatedAtUtc, p.GitHubRepositoryOwner, p.GitHubRepositoryName);

    [GeneratedRegex("^[A-Z0-9]{2,10}$")]
    private static partial Regex ProjectKeyPattern();

    [GeneratedRegex("^[A-Za-z0-9_.-]+$")]
    private static partial Regex GitHubNamePattern();
}
