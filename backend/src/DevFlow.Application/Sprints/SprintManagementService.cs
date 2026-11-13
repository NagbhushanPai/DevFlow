using DevFlow.Application.Common;
using DevFlow.Application.Common.Authorization;
using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using DevFlow.Domain.Entities;
using DevFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DevFlow.Application.Sprints;

public sealed record SprintDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    string? Goal,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc,
    SprintStatus Status,
    int IssueCount);

/// <summary>Lightweight issue row used by the backlog and sprint boards.</summary>
public sealed record BacklogIssueDto(Guid Id, int IssueNumber, string Title, IssueStatus Status, IssuePriority Priority, Guid? SprintId);

public sealed record SprintInput(string Name, string? Goal, DateTime? StartDateUtc, DateTime? EndDateUtc);

public interface ISprintManagementService
{
    Task<IReadOnlyList<SprintDto>> GetSprintsAsync(Guid projectId, CancellationToken ct);
    Task<SprintDto> CreateAsync(Guid projectId, SprintInput input, CancellationToken ct);
    Task UpdateAsync(Guid id, SprintInput input, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task StartAsync(Guid id, CancellationToken ct);
    Task CompleteAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<BacklogIssueDto>> GetBacklogAsync(Guid projectId, CancellationToken ct);
    Task<IReadOnlyList<BacklogIssueDto>> GetSprintIssuesAsync(Guid sprintId, CancellationToken ct);
    Task AssignIssueAsync(Guid sprintId, Guid issueId, CancellationToken ct);
    Task RemoveIssueAsync(Guid sprintId, Guid issueId, CancellationToken ct);
}

/// <summary>
/// Permissions: members can read; sprint lifecycle (create/edit/delete/start/complete) needs Manager;
/// moving issues in or out of a sprint needs Developer.
/// </summary>
public sealed class SprintManagementService(IApplicationDbContext context, IProjectAuthorizationService authorization)
    : ISprintManagementService
{
    public const int NameMaxLength = 200;
    public const int GoalMaxLength = 1000;

    public async Task<IReadOnlyList<SprintDto>> GetSprintsAsync(Guid projectId, CancellationToken ct)
    {
        await authorization.RequireRoleAsync(projectId, ProjectMemberRole.Viewer, ct);

        return await context.Sprints
            .Where(sprint => sprint.ProjectId == projectId)
            .OrderByDescending(sprint => sprint.CreatedAtUtc)
            .Select(sprint => new SprintDto(
                sprint.Id,
                sprint.ProjectId,
                sprint.Name,
                sprint.Goal,
                sprint.StartDateUtc,
                sprint.EndDateUtc,
                sprint.Status,
                sprint.Issues.Count(issue => !issue.IsDeleted)))
            .ToListAsync(ct);
    }

    public async Task<SprintDto> CreateAsync(Guid projectId, SprintInput input, CancellationToken ct)
    {
        await authorization.RequireRoleAsync(projectId, ProjectMemberRole.Manager, ct);
        Guard.ValidDateRange(input.StartDateUtc, input.EndDateUtc);

        var sprint = new Sprint
        {
            ProjectId = projectId,
            Name = Guard.Required(input.Name, "Sprint name", NameMaxLength),
            Goal = Guard.Optional(input.Goal, "Sprint goal", GoalMaxLength),
            StartDateUtc = input.StartDateUtc,
            EndDateUtc = input.EndDateUtc,
            Status = SprintStatus.Planned,
            CreatedAtUtc = DateTime.UtcNow,
        };
        context.Sprints.Add(sprint);
        await context.SaveChangesAsync(ct);

        return new SprintDto(sprint.Id, sprint.ProjectId, sprint.Name, sprint.Goal, sprint.StartDateUtc, sprint.EndDateUtc, sprint.Status, 0);
    }

    public async Task UpdateAsync(Guid id, SprintInput input, CancellationToken ct)
    {
        var sprint = await FindAsync(id, ct);
        await authorization.RequireRoleAsync(sprint.ProjectId, ProjectMemberRole.Manager, ct);
        if (sprint.Status != SprintStatus.Planned)
        {
            throw new ConflictException("Only planned sprints can be edited.");
        }

        Guard.ValidDateRange(input.StartDateUtc, input.EndDateUtc);
        sprint.Name = Guard.Required(input.Name, "Sprint name", NameMaxLength);
        sprint.Goal = Guard.Optional(input.Goal, "Sprint goal", GoalMaxLength);
        sprint.StartDateUtc = input.StartDateUtc;
        sprint.EndDateUtc = input.EndDateUtc;
        sprint.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var sprint = await FindAsync(id, ct);
        await authorization.RequireRoleAsync(sprint.ProjectId, ProjectMemberRole.Manager, ct);
        if (sprint.Status == SprintStatus.Active)
        {
            throw new ConflictException("An active sprint cannot be deleted.");
        }

        // Return the sprint's issues to the backlog before deleting it (the FK is NO ACTION).
        var issues = await context.Issues.Where(issue => issue.SprintId == id).ToListAsync(ct);
        foreach (var issue in issues)
        {
            issue.SprintId = null;
        }

        context.Sprints.Remove(sprint);
        await context.SaveChangesAsync(ct);
    }

    public async Task StartAsync(Guid id, CancellationToken ct)
    {
        var sprint = await FindAsync(id, ct);
        await authorization.RequireRoleAsync(sprint.ProjectId, ProjectMemberRole.Manager, ct);
        if (sprint.Status != SprintStatus.Planned)
        {
            throw new ConflictException("Only planned sprints can start.");
        }

        if (await context.Sprints.AnyAsync(s => s.ProjectId == sprint.ProjectId && s.Status == SprintStatus.Active && s.Id != id, ct))
        {
            throw new ConflictException("The project already has an active sprint.");
        }

        sprint.Status = SprintStatus.Active;
        sprint.StartDateUtc ??= DateTime.UtcNow;
        sprint.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    public async Task CompleteAsync(Guid id, CancellationToken ct)
    {
        var sprint = await FindAsync(id, ct);
        await authorization.RequireRoleAsync(sprint.ProjectId, ProjectMemberRole.Manager, ct);
        if (sprint.Status != SprintStatus.Active)
        {
            throw new ConflictException("Only active sprints can complete.");
        }

        sprint.Status = SprintStatus.Completed;
        sprint.EndDateUtc ??= DateTime.UtcNow;
        sprint.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<BacklogIssueDto>> GetBacklogAsync(Guid projectId, CancellationToken ct)
    {
        await authorization.RequireRoleAsync(projectId, ProjectMemberRole.Viewer, ct);

        return await context.Issues
            .Where(issue => issue.ProjectId == projectId && issue.SprintId == null && !issue.IsDeleted)
            .OrderBy(issue => issue.IssueNumber)
            .Select(issue => new BacklogIssueDto(issue.Id, issue.IssueNumber, issue.Title, issue.Status, issue.Priority, issue.SprintId))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<BacklogIssueDto>> GetSprintIssuesAsync(Guid sprintId, CancellationToken ct)
    {
        var sprint = await FindAsync(sprintId, ct);
        await authorization.RequireRoleAsync(sprint.ProjectId, ProjectMemberRole.Viewer, ct);

        return await context.Issues
            .Where(issue => issue.SprintId == sprintId && !issue.IsDeleted)
            .OrderBy(issue => issue.IssueNumber)
            .Select(issue => new BacklogIssueDto(issue.Id, issue.IssueNumber, issue.Title, issue.Status, issue.Priority, issue.SprintId))
            .ToListAsync(ct);
    }

    public async Task AssignIssueAsync(Guid sprintId, Guid issueId, CancellationToken ct)
    {
        var sprint = await FindAsync(sprintId, ct);
        await authorization.RequireRoleAsync(sprint.ProjectId, ProjectMemberRole.Developer, ct);
        if (sprint.Status == SprintStatus.Completed)
        {
            throw new ConflictException("Issues cannot be added to a completed sprint.");
        }

        var issue = await FindIssueAsync(issueId, ct);
        if (issue.ProjectId != sprint.ProjectId)
        {
            throw new BadRequestException("The issue must belong to the sprint project.");
        }

        issue.SprintId = sprintId;
        issue.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    public async Task RemoveIssueAsync(Guid sprintId, Guid issueId, CancellationToken ct)
    {
        var sprint = await FindAsync(sprintId, ct);
        await authorization.RequireRoleAsync(sprint.ProjectId, ProjectMemberRole.Developer, ct);

        var issue = await FindIssueAsync(issueId, ct);
        if (issue.SprintId != sprintId)
        {
            throw new NotFoundException("The issue is not in this sprint.");
        }

        issue.SprintId = null;
        issue.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    private async Task<Sprint> FindAsync(Guid id, CancellationToken ct) =>
        await context.Sprints.SingleOrDefaultAsync(sprint => sprint.Id == id, ct)
        ?? throw new NotFoundException("Sprint was not found.");

    private async Task<Issue> FindIssueAsync(Guid id, CancellationToken ct) =>
        await context.Issues.SingleOrDefaultAsync(issue => issue.Id == id && !issue.IsDeleted, ct)
        ?? throw new NotFoundException("Issue was not found.");
}
