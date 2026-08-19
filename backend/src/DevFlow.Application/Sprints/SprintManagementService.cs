using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using DevFlow.Domain.Entities;
using DevFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DevFlow.Application.Sprints;

public sealed record SprintDto(Guid Id, Guid ProjectId, string Name, string? Goal, DateTime? StartDateUtc, DateTime? EndDateUtc, SprintStatus Status, int IssueCount);

public interface ISprintManagementService
{
    Task<IReadOnlyList<SprintDto>> GetSprintsAsync(Guid projectId, CancellationToken ct);
    Task<SprintDto> CreateAsync(Guid projectId, string name, string? goal, DateTime? startDateUtc, DateTime? endDateUtc, CancellationToken ct);
    Task UpdateAsync(Guid id, string name, string? goal, DateTime? startDateUtc, DateTime? endDateUtc, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task StartAsync(Guid id, CancellationToken ct);
    Task CompleteAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<IssueDto>> GetBacklogAsync(Guid projectId, CancellationToken ct);
    Task AssignIssueAsync(Guid sprintId, Guid issueId, CancellationToken ct);
}

public sealed record IssueDto(Guid Id, int IssueNumber, string Title, IssueStatus Status, IssuePriority Priority, Guid? SprintId);

public sealed class SprintManagementService(IApplicationDbContext context, ICurrentUserService currentUser) : ISprintManagementService
{
    public async Task<IReadOnlyList<SprintDto>> GetSprintsAsync(Guid projectId, CancellationToken ct) { await AccessAsync(projectId, ct); return await context.Sprints.Where(s => s.ProjectId == projectId).OrderByDescending(s => s.CreatedAtUtc).Select(s => ToDto(s)).ToListAsync(ct); }
    public async Task<SprintDto> CreateAsync(Guid projectId, string name, string? goal, DateTime? start, DateTime? end, CancellationToken ct) { await AccessAsync(projectId, ct); var sprint = new Sprint { ProjectId = projectId, Name = Required(name), Goal = Optional(goal), StartDateUtc = start, EndDateUtc = end, Status = SprintStatus.Planned, CreatedAtUtc = DateTime.UtcNow }; context.Sprints.Add(sprint); await context.SaveChangesAsync(ct); return ToDto(sprint); }
    public async Task UpdateAsync(Guid id, string name, string? goal, DateTime? start, DateTime? end, CancellationToken ct) { var sprint = await FindAsync(id, ct); await AccessAsync(sprint.ProjectId, ct); if (sprint.Status is SprintStatus.Active or SprintStatus.Completed) throw new ArgumentException("Only planned sprints can be edited."); sprint.Name = Required(name); sprint.Goal = Optional(goal); sprint.StartDateUtc = start; sprint.EndDateUtc = end; sprint.UpdatedAtUtc = DateTime.UtcNow; await context.SaveChangesAsync(ct); }
    public async Task DeleteAsync(Guid id, CancellationToken ct) { var sprint = await FindAsync(id, ct); await AccessAsync(sprint.ProjectId, ct); if (sprint.Status == SprintStatus.Active) throw new ArgumentException("An active sprint cannot be deleted."); context.Sprints.Remove(sprint); await context.SaveChangesAsync(ct); }
    public async Task StartAsync(Guid id, CancellationToken ct) { var sprint = await FindAsync(id, ct); await AccessAsync(sprint.ProjectId, ct); if (await context.Sprints.AnyAsync(s => s.ProjectId == sprint.ProjectId && s.Status == SprintStatus.Active && s.Id != id, ct)) throw new ArgumentException("The project already has an active sprint."); if (sprint.Status != SprintStatus.Planned) throw new ArgumentException("Only planned sprints can start."); sprint.Status = SprintStatus.Active; sprint.StartDateUtc ??= DateTime.UtcNow; sprint.UpdatedAtUtc = DateTime.UtcNow; await context.SaveChangesAsync(ct); }
    public async Task CompleteAsync(Guid id, CancellationToken ct) { var sprint = await FindAsync(id, ct); await AccessAsync(sprint.ProjectId, ct); if (sprint.Status != SprintStatus.Active) throw new ArgumentException("Only active sprints can complete."); sprint.Status = SprintStatus.Completed; sprint.EndDateUtc ??= DateTime.UtcNow; sprint.UpdatedAtUtc = DateTime.UtcNow; await context.SaveChangesAsync(ct); }
    public async Task<IReadOnlyList<IssueDto>> GetBacklogAsync(Guid projectId, CancellationToken ct) { await AccessAsync(projectId, ct); return await context.Issues.Where(i => i.ProjectId == projectId && i.SprintId == null && !i.IsDeleted).OrderBy(i => i.IssueNumber).Select(i => new IssueDto(i.Id, i.IssueNumber, i.Title, i.Status, i.Priority, i.SprintId)).ToListAsync(ct); }
    public async Task AssignIssueAsync(Guid sprintId, Guid issueId, CancellationToken ct) { var sprint = await FindAsync(sprintId, ct); await AccessAsync(sprint.ProjectId, ct); var issue = await context.Issues.SingleOrDefaultAsync(i => i.Id == issueId && !i.IsDeleted, ct) ?? throw new NotFoundException("Issue was not found."); if (issue.ProjectId != sprint.ProjectId) throw new ArgumentException("The issue must belong to the sprint project."); issue.SprintId = sprintId; issue.UpdatedAtUtc = DateTime.UtcNow; await context.SaveChangesAsync(ct); }
    private Guid UserId => currentUser.UserId ?? throw new UnauthorizedAccessException();
    private async Task<Sprint> FindAsync(Guid id, CancellationToken ct) => await context.Sprints.SingleOrDefaultAsync(s => s.Id == id, ct) ?? throw new NotFoundException("Sprint was not found.");
    private async Task AccessAsync(Guid projectId, CancellationToken ct) { if (!await context.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == UserId, ct)) throw new ForbiddenAccessException("Project membership is required."); }
    private static SprintDto ToDto(Sprint s) => new(s.Id, s.ProjectId, s.Name, s.Goal, s.StartDateUtc, s.EndDateUtc, s.Status, s.Issues.Count);
    private static string Required(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Sprint name is required.") : value.Trim();
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
