using DevFlow.Application.Issues;
using DevFlow.Application.Notifications;

namespace DevFlow.Application.Common.Interfaces;

public interface IRealtimeNotifier
{
    Task IssueCreatedAsync(Guid projectId, IssueDto issue, CancellationToken ct);

    Task IssueUpdatedAsync(Guid projectId, IssueDto issue, CancellationToken ct);

    Task IssueDeletedAsync(Guid projectId, Guid issueId, CancellationToken ct);

    Task CommentAddedAsync(Guid projectId, CommentDto comment, CancellationToken ct);

    Task CommentDeletedAsync(Guid projectId, Guid issueId, Guid commentId, CancellationToken ct);

    Task NotificationCreatedAsync(Guid userId, NotificationDto notification, CancellationToken ct);
}
