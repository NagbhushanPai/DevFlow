namespace DevFlow.Application.Projects.DTOs;

public sealed record ProjectSummaryDto(
    Guid Id,
    string Name,
    string Key,
    string? Description,
    DateTime CreatedAt
);