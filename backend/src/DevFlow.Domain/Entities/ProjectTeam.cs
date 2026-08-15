using DevFlow.Domain.Common;

namespace DevFlow.Domain.Entities;

public class ProjectTeam : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public DateTime AssignedAtUtc { get; set; }
}
