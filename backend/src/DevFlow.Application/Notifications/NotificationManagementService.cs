using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using DevFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevFlow.Application.Notifications;

public sealed record NotificationDto(Guid Id, string Title, string Message, bool IsRead, DateTime CreatedAtUtc);

public interface INotificationManagementService
{
    Task<IReadOnlyList<NotificationDto>> GetMyNotificationsAsync(CancellationToken ct);
    Task<int> GetUnreadCountAsync(CancellationToken ct);
    Task MarkAsReadAsync(Guid notificationId, CancellationToken ct);
}

public sealed class NotificationManagementService(IApplicationDbContext context, ICurrentUserService currentUser) : INotificationManagementService
{
    public async Task<IReadOnlyList<NotificationDto>> GetMyNotificationsAsync(CancellationToken ct) =>
        await context.Notifications.Where(n => n.UserId == UserId).OrderByDescending(n => n.CreatedAtUtc).Take(50).Select(n => ToDto(n)).ToListAsync(ct);

    public Task<int> GetUnreadCountAsync(CancellationToken ct) =>
        context.Notifications.CountAsync(n => n.UserId == UserId && !n.IsRead, ct);

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken ct)
    {
        var notification = await context.Notifications.SingleOrDefaultAsync(n => n.Id == notificationId && n.UserId == UserId, ct) ?? throw new NotFoundException("Notification was not found.");
        if (notification.IsRead) return;
        notification.IsRead = true; notification.ReadAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    private Guid UserId => currentUser.UserId ?? throw new UnauthorizedAccessException();
    private static NotificationDto ToDto(Notification n) => new(n.Id, n.Title, n.Message, n.IsRead, n.CreatedAtUtc);
}
