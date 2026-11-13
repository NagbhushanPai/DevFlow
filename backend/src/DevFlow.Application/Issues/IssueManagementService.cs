using DevFlow.Application.Common;
using DevFlow.Application.Common.Authorization;
using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Notifications;
using DevFlow.Domain.Entities;
using DevFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DevFlow.Application.Issues;

public sealed record IssuePage(IReadOnlyList<IssueDto> Items, int Page, int PageSize, int TotalCount);

public sealed record IssueDto(
    Guid Id,
    Guid ProjectId,
    int IssueNumber,
    string Title,
    string? Description,
    IssueType Type,
    IssueStatus Status,
    IssuePriority Priority,
    Guid ReporterId,
    Guid? AssigneeId,
    Guid? SprintId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CommentDto(Guid Id, Guid IssueId, Guid AuthorId, string Content, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);

public sealed record HistoryDto(Guid Id, Guid IssueId, string FieldName, string? OldValue, string? NewValue, Guid ChangedById, DateTime CreatedAtUtc);

public sealed record IssueQuery(
    string? Search,
    IssueStatus? Status,
    IssuePriority? Priority,
    Guid? AssigneeId,
    string? SortBy,
    bool Descending,
    int Page,
    int PageSize);

public sealed record IssueInput(
    string Title,
    string? Description,
    IssueType Type,
    IssueStatus Status,
    IssuePriority Priority,
    Guid? AssigneeId);

public interface IIssueManagementService
{
    Task<IssuePage> GetIssuesAsync(Guid projectId, IssueQuery query, CancellationToken ct);
    Task<IssueDto> GetIssueAsync(Guid issueId, CancellationToken ct);
    Task<IssueDto> CreateIssueAsync(Guid projectId, IssueInput input, CancellationToken ct);
    Task UpdateIssueAsync(Guid issueId, IssueInput input, CancellationToken ct);
    Task DeleteIssueAsync(Guid issueId, CancellationToken ct);
    Task<IReadOnlyList<CommentDto>> GetCommentsAsync(Guid issueId, CancellationToken ct);
    Task<CommentDto> AddCommentAsync(Guid issueId, string content, CancellationToken ct);
    Task DeleteCommentAsync(Guid commentId, CancellationToken ct);
    Task<IReadOnlyList<HistoryDto>> GetHistoryAsync(Guid issueId, CancellationToken ct);
}

/// <summary>
/// Permissions: any project member can read; Developers and Managers can create/update issues and comment;
/// an issue can be deleted by its reporter or a Manager; a comment by its author or a Manager.
/// </summary>
public sealed class IssueManagementService(
    IApplicationDbContext context,
    IProjectAuthorizationService authorization,
    IRealtimeNotifier notifier,
    INotificationDispatcher notifications) : IIssueManagementService
{
    public const int TitleMaxLength = 300;
    public const int DescriptionMaxLength = 5000;
    public const int CommentMaxLength = 5000;
    private const int MaxIssueNumberAttempts = 10;

    private Guid UserId => authorization.CurrentUserId;

    public async Task<IssuePage> GetIssuesAsync(Guid projectId, IssueQuery request, CancellationToken ct)
    {
        await authorization.RequireRoleAsync(projectId, ProjectMemberRole.Viewer, ct);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = context.Issues.Where(issue => issue.ProjectId == projectId && !issue.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(issue => issue.Title.Contains(term)
                || (issue.Description != null && issue.Description.Contains(term)));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(issue => issue.Status == request.Status.Value);
        }

        if (request.Priority.HasValue)
        {
            query = query.Where(issue => issue.Priority == request.Priority.Value);
        }

        if (request.AssigneeId.HasValue)
        {
            query = query.Where(issue => issue.AssigneeId == request.AssigneeId.Value);
        }

        query = (request.SortBy?.ToLowerInvariant(), request.Descending) switch
        {
            ("priority", false) => query.OrderBy(issue => issue.Priority),
            ("priority", true) => query.OrderByDescending(issue => issue.Priority),
            ("status", false) => query.OrderBy(issue => issue.Status),
            ("status", true) => query.OrderByDescending(issue => issue.Status),
            ("createdat", false) => query.OrderBy(issue => issue.CreatedAtUtc),
            ("createdat", true) => query.OrderByDescending(issue => issue.CreatedAtUtc),
            (_, true) => query.OrderByDescending(issue => issue.IssueNumber),
            _ => query.OrderBy(issue => issue.IssueNumber),
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDtoExpression)
            .ToListAsync(ct);

        return new IssuePage(items, page, pageSize, total);
    }

    public async Task<IssueDto> GetIssueAsync(Guid issueId, CancellationToken ct)
    {
        var issue = await FindIssueAsync(issueId, ct);
        await authorization.RequireRoleAsync(issue.ProjectId, ProjectMemberRole.Viewer, ct);
        return ToDto(issue);
    }

    public async Task<IssueDto> CreateIssueAsync(Guid projectId, IssueInput input, CancellationToken ct)
    {
        await authorization.RequireRoleAsync(projectId, ProjectMemberRole.Developer, ct);
        await EnsureAssigneeIsMemberAsync(projectId, input.AssigneeId, ct);

        var issueNumber = await ReserveIssueNumberAsync(projectId, ct);

        var issue = new Issue
        {
            ProjectId = projectId,
            IssueNumber = issueNumber,
            Title = Guard.Required(input.Title, "Title", TitleMaxLength),
            Description = Guard.Optional(input.Description, "Description", DescriptionMaxLength),
            Type = input.Type,
            Status = IssueStatus.Backlog,
            Priority = input.Priority,
            ReporterId = UserId,
            AssigneeId = input.AssigneeId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        context.Issues.Add(issue);
        await context.SaveChangesAsync(ct);

        var dto = ToDto(issue);
        await notifier.IssueCreatedAsync(projectId, dto, ct);

        if (input.AssigneeId.HasValue && input.AssigneeId.Value != UserId)
        {
            await notifications.NotifyAsync(
                [input.AssigneeId.Value],
                "Issue assigned to you",
                $"You were assigned to issue #{issue.IssueNumber}: {issue.Title}",
                ct);
        }

        return dto;
    }

    public async Task UpdateIssueAsync(Guid issueId, IssueInput input, CancellationToken ct)
    {
        var issue = await FindIssueAsync(issueId, ct);
        await authorization.RequireRoleAsync(issue.ProjectId, ProjectMemberRole.Developer, ct);
        await EnsureAssigneeIsMemberAsync(issue.ProjectId, input.AssigneeId, ct);

        var title = Guard.Required(input.Title, "Title", TitleMaxLength);
        var description = Guard.Optional(input.Description, "Description", DescriptionMaxLength);
        var previousAssigneeId = issue.AssigneeId;

        AddHistory(issue, "Title", issue.Title, title);
        AddHistory(issue, "Description", issue.Description, description);
        AddHistory(issue, "Type", issue.Type.ToString(), input.Type.ToString());
        AddHistory(issue, "Status", issue.Status.ToString(), input.Status.ToString());
        AddHistory(issue, "Priority", issue.Priority.ToString(), input.Priority.ToString());
        AddHistory(issue, "AssigneeId", issue.AssigneeId?.ToString(), input.AssigneeId?.ToString());

        issue.Title = title;
        issue.Description = description;
        issue.Type = input.Type;
        issue.Status = input.Status;
        issue.Priority = input.Priority;
        issue.AssigneeId = input.AssigneeId;
        issue.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        await notifier.IssueUpdatedAsync(issue.ProjectId, ToDto(issue), ct);

        if (input.AssigneeId.HasValue && input.AssigneeId != previousAssigneeId && input.AssigneeId.Value != UserId)
        {
            await notifications.NotifyAsync(
                [input.AssigneeId.Value],
                "Issue assigned to you",
                $"You were assigned to issue #{issue.IssueNumber}: {issue.Title}",
                ct);
        }
    }

    public async Task DeleteIssueAsync(Guid issueId, CancellationToken ct)
    {
        var issue = await FindIssueAsync(issueId, ct);
        var role = await authorization.RequireRoleAsync(issue.ProjectId, ProjectMemberRole.Developer, ct);
        if (role != ProjectMemberRole.Manager && issue.ReporterId != UserId)
        {
            throw new ForbiddenAccessException("Only the reporter or a project manager can delete this issue.");
        }

        var now = DateTime.UtcNow;
        issue.IsDeleted = true;
        issue.DeletedAtUtc = now;
        issue.UpdatedAtUtc = now;
        await context.SaveChangesAsync(ct);

        await notifier.IssueDeletedAsync(issue.ProjectId, issue.Id, ct);
    }

    public async Task<IReadOnlyList<CommentDto>> GetCommentsAsync(Guid issueId, CancellationToken ct)
    {
        var issue = await FindIssueAsync(issueId, ct);
        await authorization.RequireRoleAsync(issue.ProjectId, ProjectMemberRole.Viewer, ct);

        return await context.Comments
            .Where(comment => comment.IssueId == issueId && !comment.IsDeleted)
            .OrderBy(comment => comment.CreatedAtUtc)
            .Select(comment => new CommentDto(comment.Id, comment.IssueId, comment.AuthorId, comment.Content, comment.CreatedAtUtc, comment.UpdatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<CommentDto> AddCommentAsync(Guid issueId, string content, CancellationToken ct)
    {
        var issue = await FindIssueAsync(issueId, ct);
        await authorization.RequireRoleAsync(issue.ProjectId, ProjectMemberRole.Developer, ct);

        var comment = new Comment
        {
            IssueId = issueId,
            AuthorId = UserId,
            Content = Guard.Required(content, "Comment", CommentMaxLength),
            CreatedAtUtc = DateTime.UtcNow,
        };
        context.Comments.Add(comment);
        await context.SaveChangesAsync(ct);

        var dto = new CommentDto(comment.Id, issueId, comment.AuthorId, comment.Content, comment.CreatedAtUtc, comment.UpdatedAtUtc);
        await notifier.CommentAddedAsync(issue.ProjectId, dto, ct);

        var recipients = new[] { issue.ReporterId, issue.AssigneeId }
            .Where(id => id.HasValue && id.Value != UserId)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        await notifications.NotifyAsync(
            recipients,
            "New comment",
            $"New comment on issue #{issue.IssueNumber}: {issue.Title}",
            ct);

        return dto;
    }

    public async Task DeleteCommentAsync(Guid commentId, CancellationToken ct)
    {
        var comment = await context.Comments.SingleOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted, ct)
            ?? throw new NotFoundException("Comment was not found.");
        var issue = await FindIssueAsync(comment.IssueId, ct);
        var role = await authorization.RequireRoleAsync(issue.ProjectId, ProjectMemberRole.Viewer, ct);

        if (comment.AuthorId != UserId && role != ProjectMemberRole.Manager)
        {
            throw new ForbiddenAccessException("Only the comment author or a project manager can delete it.");
        }

        comment.IsDeleted = true;
        comment.DeletedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        await notifier.CommentDeletedAsync(issue.ProjectId, issue.Id, commentId, ct);
    }

    public async Task<IReadOnlyList<HistoryDto>> GetHistoryAsync(Guid issueId, CancellationToken ct)
    {
        var issue = await FindIssueAsync(issueId, ct);
        await authorization.RequireRoleAsync(issue.ProjectId, ProjectMemberRole.Viewer, ct);

        return await context.IssueHistories
            .Where(history => history.IssueId == issueId)
            .OrderByDescending(history => history.CreatedAtUtc)
            .Select(history => new HistoryDto(history.Id, history.IssueId, history.FieldName, history.OldValue, history.NewValue, history.ChangedById, history.CreatedAtUtc))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Atomically claims the project's next issue number. Project.NextIssueNumber is an optimistic-concurrency
    /// token, so when two requests race the loser reloads and retries with the next free number. Only the counter
    /// is saved here, which keeps the retry safe on every provider; if the later insert fails, the number is
    /// simply skipped (gaps are acceptable, duplicates are not).
    /// </summary>
    private async Task<int> ReserveIssueNumberAsync(Guid projectId, CancellationToken ct)
    {
        var project = await context.Projects.SingleOrDefaultAsync(p => p.Id == projectId, ct)
            ?? throw new NotFoundException("Project was not found.");

        for (var attempt = 1; ; attempt++)
        {
            var number = project.NextIssueNumber;
            project.NextIssueNumber = number + 1;

            try
            {
                await context.SaveChangesAsync(ct);
                return number;
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < MaxIssueNumberAttempts)
            {
                // Small random backoff so a burst of concurrent creators doesn't collide on every retry.
                await Task.Delay(Random.Shared.Next(5, 20 * attempt), ct);
                foreach (var entry in ex.Entries)
                {
                    await entry.ReloadAsync(ct);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("The project is busy; please retry creating the issue.");
            }
        }
    }

    private async Task<Issue> FindIssueAsync(Guid id, CancellationToken ct) =>
        await context.Issues.SingleOrDefaultAsync(issue => issue.Id == id && !issue.IsDeleted, ct)
        ?? throw new NotFoundException("Issue was not found.");

    private async Task EnsureAssigneeIsMemberAsync(Guid projectId, Guid? assigneeId, CancellationToken ct)
    {
        if (assigneeId.HasValue
            && !await context.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == assigneeId.Value, ct))
        {
            throw new BadRequestException("The assignee must be a project member.");
        }
    }

    private void AddHistory(Issue issue, string field, string? oldValue, string? newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        context.IssueHistories.Add(new IssueHistory
        {
            IssueId = issue.Id,
            FieldName = field,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedById = UserId,
            CreatedAtUtc = DateTime.UtcNow,
        });
    }

    private static readonly System.Linq.Expressions.Expression<Func<Issue, IssueDto>> ToDtoExpression = i =>
        new IssueDto(i.Id, i.ProjectId, i.IssueNumber, i.Title, i.Description, i.Type, i.Status, i.Priority, i.ReporterId, i.AssigneeId, i.SprintId, i.CreatedAtUtc, i.UpdatedAtUtc);

    private static IssueDto ToDto(Issue i) =>
        new(i.Id, i.ProjectId, i.IssueNumber, i.Title, i.Description, i.Type, i.Status, i.Priority, i.ReporterId, i.AssigneeId, i.SprintId, i.CreatedAtUtc, i.UpdatedAtUtc);
}
