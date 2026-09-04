using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Issues;
using DevFlow.Application.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace DevFlow.API.Hubs;

public sealed class SignalRRealtimeNotifier(IHubContext<IssueHub> hub) : IRealtimeNotifier
{
    public Task IssueCreatedAsync(Guid projectId, IssueDto issue, CancellationToken ct) =>
        hub.Clients.Group(IssueHub.GroupName(projectId)).SendAsync("issueCreated", issue, ct);

    public Task IssueUpdatedAsync(Guid projectId, IssueDto issue, CancellationToken ct) =>
        hub.Clients.Group(IssueHub.GroupName(projectId)).SendAsync("issueUpdated", issue, ct);

    public Task IssueDeletedAsync(Guid projectId, Guid issueId, CancellationToken ct) =>
        hub.Clients.Group(IssueHub.GroupName(projectId)).SendAsync("issueDeleted", issueId, ct);

    public Task CommentAddedAsync(Guid projectId, CommentDto comment, CancellationToken ct) =>
        hub.Clients.Group(IssueHub.GroupName(projectId)).SendAsync("commentAdded", comment, ct);

    public Task CommentDeletedAsync(Guid projectId, Guid issueId, Guid commentId, CancellationToken ct) =>
        hub.Clients.Group(IssueHub.GroupName(projectId)).SendAsync("commentDeleted", new { issueId, commentId }, ct);

    public Task NotificationCreatedAsync(Guid userId, NotificationDto notification, CancellationToken ct) =>
        hub.Clients.User(userId.ToString()).SendAsync("notificationCreated", notification, ct);
}
