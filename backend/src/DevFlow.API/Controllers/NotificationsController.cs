using DevFlow.Application.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace DevFlow.API.Controllers;

[Route("api/notifications")]
public sealed class NotificationsController(INotificationManagementService notifications) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetMine(CancellationToken ct) =>
        Ok(await notifications.GetMyNotificationsAsync(ct));

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken ct) =>
        Ok(await notifications.GetUnreadCountAsync(ct));

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        await notifications.MarkAsReadAsync(id, ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        await notifications.MarkAllAsReadAsync(ct);
        return NoContent();
    }
}
