using DevFlow.Domain.Common;

namespace DevFlow.Domain.Entities;

public class IssueHistory : BaseEntity
{
    public string FieldName { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public Guid IssueId { get; set; }

    public Guid ChangedById { get; set; }

    public Issue Issue { get; set; } = null!;
}