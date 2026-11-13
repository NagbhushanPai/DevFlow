namespace DevFlow.Application.Organizations.DTOs;

public sealed record OrganizationDto(Guid Id, string Name, string? Description, Guid OwnerId, string? MyRole = null);

public sealed record OrganizationMemberDto(Guid UserId, string Role, string FullName, string Email);

public sealed record TeamDto(Guid Id, Guid OrganizationId, string Name, string? Description);

public sealed record TeamMemberDto(Guid UserId, string Role, string FullName, string Email);
