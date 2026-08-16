using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using DevFlow.Domain.Entities;
using DevFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DevFlow.Application.Issues;

public sealed record IssuePage(IReadOnlyList<IssueDto> Items, int Page, int PageSize, int TotalCount);
public sealed record IssueDto(Guid Id, Guid ProjectId, int IssueNumber, string Title, string? Description, IssueType Type, IssueStatus Status, IssuePriority Priority, Guid ReporterId, Guid? AssigneeId, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
public sealed record CommentDto(Guid Id, Guid IssueId, Guid AuthorId, string Content, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
public sealed record HistoryDto(Guid Id, Guid IssueId, string FieldName, string? OldValue, string? NewValue, Guid ChangedById, DateTime CreatedAtUtc);

public interface IIssueManagementService
{
    Task<IssuePage> GetIssuesAsync(Guid projectId, string? search, IssueStatus? status, IssuePriority? priority, Guid? assigneeId, string? sortBy, bool descending, int page, int pageSize, CancellationToken ct);
    Task<IssueDto> GetIssueAsync(Guid issueId, CancellationToken ct);
    Task<IssueDto> CreateIssueAsync(Guid projectId, string title, string? description, IssueType type, IssuePriority priority, Guid? assigneeId, CancellationToken ct);
    Task UpdateIssueAsync(Guid issueId, string title, string? description, IssueType type, IssueStatus status, IssuePriority priority, Guid? assigneeId, CancellationToken ct);
    Task DeleteIssueAsync(Guid issueId, CancellationToken ct);
    Task<IReadOnlyList<CommentDto>> GetCommentsAsync(Guid issueId, CancellationToken ct);
    Task<CommentDto> AddCommentAsync(Guid issueId, string content, CancellationToken ct);
    Task DeleteCommentAsync(Guid commentId, CancellationToken ct);
    Task<IReadOnlyList<HistoryDto>> GetHistoryAsync(Guid issueId, CancellationToken ct);
}

public sealed class IssueManagementService(IApplicationDbContext context, ICurrentUserService currentUser) : IIssueManagementService
{
    public async Task<IssuePage> GetIssuesAsync(Guid projectId, string? search, IssueStatus? status, IssuePriority? priority, Guid? assigneeId, string? sortBy, bool descending, int page, int pageSize, CancellationToken ct)
    {
        await RequireProjectAccessAsync(projectId, ct); page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = context.Issues.Where(i => i.ProjectId == projectId && !i.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); query = query.Where(i => i.Title.Contains(term) || (i.Description != null && i.Description.Contains(term))); }
        if (status.HasValue) query = query.Where(i => i.Status == status.Value);
        if (priority.HasValue) query = query.Where(i => i.Priority == priority.Value);
        if (assigneeId.HasValue) query = query.Where(i => i.AssigneeId == assigneeId.Value);
        query = (sortBy?.ToLowerInvariant()) switch { "priority" => descending ? query.OrderByDescending(i => i.Priority) : query.OrderBy(i => i.Priority), "status" => descending ? query.OrderByDescending(i => i.Status) : query.OrderBy(i => i.Status), "createdat" => descending ? query.OrderByDescending(i => i.CreatedAtUtc) : query.OrderBy(i => i.CreatedAtUtc), _ => descending ? query.OrderByDescending(i => i.IssueNumber) : query.OrderBy(i => i.IssueNumber) };
        var total = await query.CountAsync(ct); var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(i => ToDto(i)).ToListAsync(ct);
        return new IssuePage(items, page, pageSize, total);
    }

    public async Task<IssueDto> GetIssueAsync(Guid issueId, CancellationToken ct) { var issue = await FindAsync(issueId, ct); await RequireProjectAccessAsync(issue.ProjectId, ct); return ToDto(issue); }

    public async Task<IssueDto> CreateIssueAsync(Guid projectId, string title, string? description, IssueType type, IssuePriority priority, Guid? assigneeId, CancellationToken ct)
    {
        await RequireProjectAccessAsync(projectId, ct); var project = await context.Projects.SingleOrDefaultAsync(p => p.Id == projectId, ct) ?? throw new NotFoundException("Project was not found.");
        if (assigneeId.HasValue && !await context.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == assigneeId.Value, ct)) throw new ArgumentException("The assignee must be a project member.");
        var issue = new Issue { ProjectId = projectId, IssueNumber = project.NextIssueNumber++, Title = Required(title), Description = Optional(description), Type = type, Status = IssueStatus.Backlog, Priority = priority, ReporterId = UserId, AssigneeId = assigneeId, CreatedAtUtc = DateTime.UtcNow };
        context.Issues.Add(issue); await context.SaveChangesAsync(ct); return ToDto(issue);
    }

    public async Task UpdateIssueAsync(Guid issueId, string title, string? description, IssueType type, IssueStatus status, IssuePriority priority, Guid? assigneeId, CancellationToken ct)
    {
        var issue = await FindAsync(issueId, ct); await RequireProjectAccessAsync(issue.ProjectId, ct);
        if (assigneeId.HasValue && !await context.ProjectMembers.AnyAsync(m => m.ProjectId == issue.ProjectId && m.UserId == assigneeId.Value, ct)) throw new ArgumentException("The assignee must be a project member.");
        AddHistory(issue, "Title", issue.Title, Required(title)); AddHistory(issue, "Description", issue.Description, Optional(description)); AddHistory(issue, "Type", issue.Type.ToString(), type.ToString()); AddHistory(issue, "Status", issue.Status.ToString(), status.ToString()); AddHistory(issue, "Priority", issue.Priority.ToString(), priority.ToString()); AddHistory(issue, "AssigneeId", issue.AssigneeId?.ToString(), assigneeId?.ToString());
        issue.Title = Required(title); issue.Description = Optional(description); issue.Type = type; issue.Status = status; issue.Priority = priority; issue.AssigneeId = assigneeId; issue.UpdatedAtUtc = DateTime.UtcNow; await context.SaveChangesAsync(ct);
    }

    public async Task DeleteIssueAsync(Guid issueId, CancellationToken ct) { var issue = await FindAsync(issueId, ct); await RequireProjectAccessAsync(issue.ProjectId, ct); issue.IsDeleted = true; issue.DeletedAtUtc = DateTime.UtcNow; issue.UpdatedAtUtc = DateTime.UtcNow; await context.SaveChangesAsync(ct); }
    public async Task<IReadOnlyList<CommentDto>> GetCommentsAsync(Guid issueId, CancellationToken ct) { await AccessIssueAsync(issueId, ct); return await context.Comments.Where(c => c.IssueId == issueId && !c.IsDeleted).OrderBy(c => c.CreatedAtUtc).Select(c => new CommentDto(c.Id, c.IssueId, c.AuthorId, c.Content, c.CreatedAtUtc, c.UpdatedAtUtc)).ToListAsync(ct); }
    public async Task<CommentDto> AddCommentAsync(Guid issueId, string content, CancellationToken ct) { await AccessIssueAsync(issueId, ct); var comment = new Comment { IssueId = issueId, AuthorId = UserId, Content = Required(content), CreatedAtUtc = DateTime.UtcNow }; context.Comments.Add(comment); await context.SaveChangesAsync(ct); return new CommentDto(comment.Id, issueId, comment.AuthorId, comment.Content, comment.CreatedAtUtc, comment.UpdatedAtUtc); }
    public async Task DeleteCommentAsync(Guid commentId, CancellationToken ct) { var comment = await context.Comments.SingleOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted, ct) ?? throw new NotFoundException("Comment was not found."); await AccessIssueAsync(comment.IssueId, ct); if (comment.AuthorId != UserId) throw new ForbiddenAccessException("Only the comment author can delete it."); comment.IsDeleted = true; comment.DeletedAtUtc = DateTime.UtcNow; await context.SaveChangesAsync(ct); }
    public async Task<IReadOnlyList<HistoryDto>> GetHistoryAsync(Guid issueId, CancellationToken ct) { await AccessIssueAsync(issueId, ct); return await context.IssueHistories.Where(h => h.IssueId == issueId).OrderByDescending(h => h.CreatedAtUtc).Select(h => new HistoryDto(h.Id, h.IssueId, h.FieldName, h.OldValue, h.NewValue, h.ChangedById, h.CreatedAtUtc)).ToListAsync(ct); }

    private Guid UserId => currentUser.UserId ?? throw new UnauthorizedAccessException();
    private async Task<Issue> FindAsync(Guid id, CancellationToken ct) => await context.Issues.SingleOrDefaultAsync(i => i.Id == id && !i.IsDeleted, ct) ?? throw new NotFoundException("Issue was not found.");
    private async Task AccessIssueAsync(Guid id, CancellationToken ct) { var issue = await FindAsync(id, ct); await RequireProjectAccessAsync(issue.ProjectId, ct); }
    private async Task RequireProjectAccessAsync(Guid projectId, CancellationToken ct) { if (!await context.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == UserId, ct)) throw new ForbiddenAccessException("Project membership is required."); }
    private void AddHistory(Issue issue, string field, string? oldValue, string? newValue) { if (oldValue != newValue) context.IssueHistories.Add(new IssueHistory { IssueId = issue.Id, FieldName = field, OldValue = oldValue, NewValue = newValue, ChangedById = UserId, CreatedAtUtc = DateTime.UtcNow }); }
    private static IssueDto ToDto(Issue i) => new(i.Id, i.ProjectId, i.IssueNumber, i.Title, i.Description, i.Type, i.Status, i.Priority, i.ReporterId, i.AssigneeId, i.CreatedAtUtc, i.UpdatedAtUtc);
    private static string Required(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A value is required.") : value.Trim();
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
