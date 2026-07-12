using DevFlow.Domain.Common;
using DevFlow.Domain.Enums;

namespace DevFlow.Domain.Entities;

public class ProjectMember : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Guid UserId { get; set; }

    public ProjectMemberRole Role { get; set; }

    public DateTime JoinedAtUtc { get; set; }

    public Project Project { get; set; } = null!;
}