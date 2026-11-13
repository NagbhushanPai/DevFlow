using DevFlow.Application.Common.Interfaces;
using DevFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DevFlow.Application.Notifications;

public interface INotificationDispatcher
{
    /// <summary>
    /// Persists one in-app notification per recipient in a single save, pushes them in real time,
    /// and queues an email for each. Email delivery happens in the background and never blocks the caller.
    /// </summary>
    Task NotifyAsync(IReadOnlyCollection<Guid> userIds, string title, string message, CancellationToken ct);
}

public sealed class NotificationDispatcher(
    IApplicationDbContext context,
    IRealtimeNotifier notifier,
    IIdentityService identityService,
    IEmailNotificationSender emailSender,
    ILogger<NotificationDispatcher> logger) : INotificationDispatcher
{
    public async Task NotifyAsync(IReadOnlyCollection<Guid> userIds, string title, string message, CancellationToken ct)
    {
        if (userIds.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var notifications = userIds.Distinct()
            .Select(userId => new Notification { UserId = userId, Title = title, Message = message, CreatedAtUtc = now })
            .ToList();

        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync(ct);

        foreach (var notification in notifications)
        {
            var dto = new NotificationDto(notification.Id, notification.Title, notification.Message, notification.IsRead, notification.CreatedAtUtc);
            await notifier.NotificationCreatedAsync(notification.UserId, dto, ct);

            try
            {
                var user = await identityService.GetUserByIdAsync(notification.UserId, ct);
                if (user is not null)
                {
                    // The registered sender only enqueues; SMTP delivery runs in a background worker.
                    await emailSender.SendAsync(user.Email, title, message, ct);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to queue notification email for user {UserId}.", notification.UserId);
            }
        }
    }
}
