namespace DevFlow.Application.Projects.DTOs;

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string Key,
    string? Description,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);