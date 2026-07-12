using DevFlow.Domain.Common;
using DevFlow.Domain.Enums;

namespace DevFlow.Domain.Entities;

public class Sprint : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Goal { get; set; }

    public DateTime? StartDateUtc { get; set; }

    public DateTime? EndDateUtc { get; set; }

    public SprintStatus Status { get; set; }

    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public ICollection<Issue> Issues { get; set; }
        = new List<Issue>();
}