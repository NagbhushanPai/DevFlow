using DevFlow.Domain.Enums;

namespace DevFlow.Application.Projects.DTOs;

public sealed record ProjectDto(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string Key,
    string? Description,
    ProjectStatus Status,
    Guid OwnerId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    string? GitHubRepositoryOwner,
    string? GitHubRepositoryName,
    ProjectMemberRole? MyRole = null);

public sealed record ProjectPage(IReadOnlyList<ProjectDto> Items, int Page, int PageSize, int TotalCount);

public sealed record ProjectMemberDto(Guid UserId, ProjectMemberRole Role, string FullName, string Email);

public sealed record ProjectQuery(string? Search, ProjectStatus? Status, string? SortBy, bool Descending, int Page, int PageSize);

public sealed record CreateProjectInput(Guid OrganizationId, string Name, string Key, string? Description);

public sealed record UpdateProjectInput(string Name, string Key, string? Description, ProjectStatus Status);
