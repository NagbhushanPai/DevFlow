using DevFlow.Domain.Common;

namespace DevFlow.Domain.Entities;

// Teams belong to an Organization and have members
public class Team : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string? Description { get; set; }

    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();

    public ICollection<ProjectTeam> ProjectTeams { get; set; } = new List<ProjectTeam>();
}
