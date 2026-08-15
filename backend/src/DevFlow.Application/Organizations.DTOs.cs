namespace DevFlow.Application.Organizations.DTOs;

public sealed class OrganizationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
}

public sealed record OrganizationMemberDto(Guid UserId, string Role);

public sealed record TeamDto(Guid Id, Guid OrganizationId, string Name, string? Description);

public sealed record TeamMemberDto(Guid UserId, string Role);
