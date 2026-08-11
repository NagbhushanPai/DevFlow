using DevFlow.Domain.Enums;

namespace DevFlow.Application.Projects.DTOs;

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string Key,
    string? Description,
    ProjectStatus Status,
    Guid OwnerId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);