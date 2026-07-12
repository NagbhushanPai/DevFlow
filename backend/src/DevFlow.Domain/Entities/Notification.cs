using DevFlow.Domain.Common;

namespace DevFlow.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime? ReadAtUtc { get; set; }
}