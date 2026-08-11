using DevFlow.Domain.Common;

namespace DevFlow.Domain.Entities;

// Join entity linking users to organizations with a role
public class OrganizationMember : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid UserId { get; set; }

    // e.g., "Owner", "Admin", "Member"
    public string Role { get; set; } = "Member";
}
