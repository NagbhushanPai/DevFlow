using DevFlow.Domain.Common;

namespace DevFlow.Domain.Entities;

public class Comment : BaseEntity
{
    public string Content { get; set; } = string.Empty;

    public Guid IssueId { get; set; }

    public Guid AuthorId { get; set; }

    public Issue Issue { get; set; } = null!;
}