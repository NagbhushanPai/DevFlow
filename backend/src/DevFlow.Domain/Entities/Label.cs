using DevFlow.Domain.Common;

namespace DevFlow.Domain.Entities;

public class Label : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Color { get; set; }

    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public ICollection<IssueLabel> IssueLabels { get; set; }
        = new List<IssueLabel>();
}