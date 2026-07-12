using DevFlow.Domain.Common;
using DevFlow.Domain.Enums;

namespace DevFlow.Domain.Entities;

public class Issue : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public IssueType Type { get; set; }

    public IssueStatus Status { get; set; }

    public IssuePriority Priority { get; set; }

    public int IssueNumber { get; set; }

    public Guid ProjectId { get; set; }

    public Guid? SprintId { get; set; }

    public Guid ReporterId { get; set; }

    public Guid? AssigneeId { get; set; }

    public Project Project { get; set; } = null!;

    public Sprint? Sprint { get; set; }


public ICollection<Comment> Comments { get; set; }
    = new List<Comment>();

public ICollection<Attachment> Attachments { get; set; }
    = new List<Attachment>();

public ICollection<IssueHistory> History { get; set; }
    = new List<IssueHistory>();

public ICollection<IssueLabel> IssueLabels { get; set; }
    = new List<IssueLabel>();

public ICollection<IssueLink> OutgoingLinks { get; set; }
    = new List<IssueLink>();

public ICollection<IssueLink> IncomingLinks { get; set; }
    = new List<IssueLink>();
}