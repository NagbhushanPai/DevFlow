using DevFlow.Application.Common.Authorization;
using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Projects.DTOs;
using DevFlow.Application.Organizations.DTOs;
using DevFlow.Domain.Entities;
using DevFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DevFlow.Application.Projects;

public sealed record ProjectPage(IReadOnlyList<ProjectDto> Items, int Page, int PageSize, int TotalCount);

public interface IProjectManagementService
{
    Task<ProjectPage> GetProjectsAsync(string? search, ProjectStatus? status, string? sortBy, bool descending, int page, int pageSize, CancellationToken cancellationToken);
    Task<ProjectDto> GetProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task UpdateProjectAsync(Guid projectId, string name, string key, string? description, ProjectStatus status, CancellationToken cancellationToken);
    Task DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProjectMemberDto>> GetMembersAsync(Guid projectId, CancellationToken cancellationToken);
    Task SetMemberAsync(Guid projectId, Guid userId, ProjectMemberRole role, CancellationToken cancellationToken);
    Task RemoveMemberAsync(Guid projectId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TeamDto>> GetTeamsAsync(Guid projectId, CancellationToken cancellationToken);
    Task AssignTeamAsync(Guid projectId, Guid teamId, CancellationToken cancellationToken);
    Task RemoveTeamAsync(Guid projectId, Guid teamId, CancellationToken cancellationToken);
}

public sealed record ProjectMemberDto(Guid UserId, ProjectMemberRole Role);

public sealed class ProjectManagementService(IApplicationDbContext context, ICurrentUserService currentUser, IOrganizationAuthorizationService organizationAuthorization)
    : IProjectManagementService
{
    public async Task<ProjectPage> GetProjectsAsync(string? search, ProjectStatus? status, string? sortBy, bool descending, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var userId = UserId;
        var query = context.Projects.Where(project => project.Members.Any(member => member.UserId == userId));
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); query = query.Where(p => p.Name.Contains(term) || p.Key.Contains(term)); }
        if (status.HasValue) query = query.Where(p => p.Status == status);
        query = (sortBy?.ToLowerInvariant()) switch { "key" => descending ? query.OrderByDescending(p => p.Key) : query.OrderBy(p => p.Key), "createdat" => descending ? query.OrderByDescending(p => p.CreatedAtUtc) : query.OrderBy(p => p.CreatedAtUtc), _ => descending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name) };
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(p => ToDto(p)).ToListAsync(ct);
        return new ProjectPage(items, page, pageSize, total);
    }

    public async Task<ProjectDto> GetProjectAsync(Guid projectId, CancellationToken ct) { var project = await FindAsync(projectId, ct); await RequireAccessAsync(project, ct); return ToDto(project); }
    public async Task UpdateProjectAsync(Guid projectId, string name, string key, string? description, ProjectStatus status, CancellationToken ct)
    {
        var project = await FindAsync(projectId, ct); await RequireManagerAsync(project, ct);
        project.Name = Required(name, "Project name"); project.Key = Required(key, "Project key").ToUpperInvariant(); project.Description = Optional(description); project.Status = status; project.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }
    public async Task DeleteProjectAsync(Guid projectId, CancellationToken ct) { var project = await FindAsync(projectId, ct); await RequireManagerAsync(project, ct); context.Projects.Remove(project); await context.SaveChangesAsync(ct); }
    public async Task<IReadOnlyList<ProjectMemberDto>> GetMembersAsync(Guid projectId, CancellationToken ct) { var project = await FindAsync(projectId, ct); await RequireAccessAsync(project, ct); return await context.ProjectMembers.Where(m => m.ProjectId == projectId).Select(m => new ProjectMemberDto(m.UserId, m.Role)).ToListAsync(ct); }
    public async Task SetMemberAsync(Guid projectId, Guid userId, ProjectMemberRole role, CancellationToken ct)
    {
        var project = await FindAsync(projectId, ct); await RequireManagerAsync(project, ct);
        if (!await context.OrganizationMembers.AnyAsync(m => m.OrganizationId == project.OrganizationId && m.UserId == userId, ct)) throw new ArgumentException("A project member must belong to the organization.");
        var member = await context.ProjectMembers.SingleOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId, ct);
        if (member is null) context.ProjectMembers.Add(new ProjectMember { Id = Guid.NewGuid(), ProjectId = projectId, UserId = userId, Role = role, JoinedAtUtc = DateTime.UtcNow }); else member.Role = role;
        await context.SaveChangesAsync(ct);
    }
    public async Task RemoveMemberAsync(Guid projectId, Guid userId, CancellationToken ct)
    {
        var project = await FindAsync(projectId, ct); await RequireManagerAsync(project, ct);
        var member = await context.ProjectMembers.SingleOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId, ct) ?? throw new NotFoundException("Project member was not found.");
        if (member.UserId == project.OwnerId) throw new ArgumentException("The project owner cannot be removed.");
        context.ProjectMembers.Remove(member); await context.SaveChangesAsync(ct);
    }
    public async Task<IReadOnlyList<TeamDto>> GetTeamsAsync(Guid projectId, CancellationToken ct) { var project = await FindAsync(projectId, ct); await RequireAccessAsync(project, ct); return await context.ProjectTeams.Where(item => item.ProjectId == projectId).Select(item => new TeamDto(item.Team.Id, item.Team.OrganizationId, item.Team.Name, item.Team.Description)).ToListAsync(ct); }
    public async Task AssignTeamAsync(Guid projectId, Guid teamId, CancellationToken ct)
    {
        var project = await FindAsync(projectId, ct); await RequireManagerAsync(project, ct);
        var team = await context.Teams.SingleOrDefaultAsync(t => t.Id == teamId, ct) ?? throw new NotFoundException("Team was not found.");
        if (team.OrganizationId != project.OrganizationId) throw new ArgumentException("A team must belong to the project's organization.");
        if (!await context.ProjectTeams.AnyAsync(item => item.ProjectId == projectId && item.TeamId == teamId, ct)) { context.ProjectTeams.Add(new ProjectTeam { Id = Guid.NewGuid(), ProjectId = projectId, TeamId = teamId, AssignedAtUtc = DateTime.UtcNow }); await context.SaveChangesAsync(ct); }
    }
    public async Task RemoveTeamAsync(Guid projectId, Guid teamId, CancellationToken ct) { var project = await FindAsync(projectId, ct); await RequireManagerAsync(project, ct); var item = await context.ProjectTeams.SingleOrDefaultAsync(x => x.ProjectId == projectId && x.TeamId == teamId, ct) ?? throw new NotFoundException("Project team assignment was not found."); context.ProjectTeams.Remove(item); await context.SaveChangesAsync(ct); }
    private Guid UserId => currentUser.UserId ?? throw new UnauthorizedAccessException();
    private async Task<Project> FindAsync(Guid id, CancellationToken ct) => await context.Projects.SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new NotFoundException("Project was not found.");
    private async Task RequireAccessAsync(Project p, CancellationToken ct) { if (!await context.ProjectMembers.AnyAsync(m => m.ProjectId == p.Id && m.UserId == UserId, ct)) throw new ForbiddenAccessException("Project membership is required."); }
    private async Task RequireManagerAsync(Project p, CancellationToken ct) { var role = await context.ProjectMembers.Where(m => m.ProjectId == p.Id && m.UserId == UserId).Select(m => (ProjectMemberRole?)m.Role).SingleOrDefaultAsync(ct); if (role != ProjectMemberRole.Manager) await organizationAuthorization.RequireManagerAsync(p.OrganizationId, UserId, ct); }
    private static ProjectDto ToDto(Project p) => new(p.Id, p.OrganizationId, p.Name, p.Key, p.Description, p.Status, p.OwnerId, p.CreatedAtUtc, p.UpdatedAtUtc);
    private static string Required(string value, string name) { var text = value?.Trim(); return string.IsNullOrWhiteSpace(text) ? throw new ArgumentException($"{name} is required.") : text; }
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
