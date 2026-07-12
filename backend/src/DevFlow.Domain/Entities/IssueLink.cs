using DevFlow.Domain.Common;

namespace DevFlow.Domain.Entities;

public class IssueLink : BaseEntity
{
    public Guid SourceIssueId { get; set; }

    public Guid TargetIssueId { get; set; }

    public string LinkType { get; set; } = string.Empty;

    public Issue SourceIssue { get; set; } = null!;

    public Issue TargetIssue { get; set; } = null!;
}