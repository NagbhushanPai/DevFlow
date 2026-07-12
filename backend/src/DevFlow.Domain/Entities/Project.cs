using DevFlow.Domain.Common;
using DevFlow.Domain.Enums;

namespace DevFlow.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ProjectStatus Status { get; set; }

    public Guid OwnerId { get; set; }

    public ICollection<ProjectMember> Members { get; set; }
        = new List<ProjectMember>();

    public ICollection<Sprint> Sprints { get; set; }
        = new List<Sprint>();

    public ICollection<Issue> Issues { get; set; }
        = new List<Issue>();

    public ICollection<Label> Labels { get; set; }
    = new List<Label>();
}
