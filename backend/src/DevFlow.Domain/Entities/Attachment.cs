using DevFlow.Domain.Common;

namespace DevFlow.Domain.Entities;

public class Attachment : BaseEntity
{
    public string FileName { get; set; } = string.Empty;

    public string FileUrl { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public Guid IssueId { get; set; }

    public Guid UploadedById { get; set; }

    public Issue Issue { get; set; } = null!;
}