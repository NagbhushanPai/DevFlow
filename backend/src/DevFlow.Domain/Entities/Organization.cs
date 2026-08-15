using DevFlow.Domain.Common;

namespace DevFlow.Domain.Entities;

// Organization aggregate root
public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid OwnerId { get; set; }

    public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();

    public ICollection<Team> Teams { get; set; } = new List<Team>();

    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
