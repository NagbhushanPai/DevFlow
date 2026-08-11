using DevFlow.Domain.Common;

namespace DevFlow.Domain.Entities;

// Join entity linking users to teams with a role
public class TeamMember : BaseEntity
{
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public Guid UserId { get; set; }

    // e.g., "Lead", "Developer", "Viewer"
    public string Role { get; set; } = "Developer";
}
