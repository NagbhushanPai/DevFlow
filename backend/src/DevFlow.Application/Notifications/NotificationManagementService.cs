using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevFlow.Application.Notifications;

public sealed record NotificationDto(Guid Id, string Title, string Message, bool IsRead, DateTime CreatedAtUtc);

public interface INotificationManagementService
{
    Task<IReadOnlyList<NotificationDto>> GetMyNotificationsAsync(CancellationToken ct);
    Task<int> GetUnreadCountAsync(CancellationToken ct);
    Task MarkAsReadAsync(Guid notificationId, CancellationToken ct);
    Task MarkAllAsReadAsync(CancellationToken ct);
}

public sealed class NotificationManagementService(IApplicationDbContext context, ICurrentUserService currentUser)
    : INotificationManagementService
{
    private Guid UserId => currentUser.UserId ?? throw new UnauthorizedAccessException();

    public async Task<IReadOnlyList<NotificationDto>> GetMyNotificationsAsync(CancellationToken ct)
    {
        var userId = UserId;
        return await context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(50)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.IsRead, n.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public Task<int> GetUnreadCountAsync(CancellationToken ct)
    {
        var userId = UserId;
        return context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken ct)
    {
        var userId = UserId;
        var notification = await context.Notifications.SingleOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct)
            ?? throw new NotFoundException("Notification was not found.");

        if (notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        notification.ReadAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    public async Task MarkAllAsReadAsync(CancellationToken ct)
    {
        var userId = UserId;
        var unread = await context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(ct);
        if (unread.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = now;
        }

        await context.SaveChangesAsync(ct);
    }
}
