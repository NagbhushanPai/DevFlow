using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Organizations.DTOs;
using DevFlow.Application.Projects;
using DevFlow.Application.Projects.DTOs;
using DevFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace DevFlow.API.Controllers;

[Route("api/projects")]
public sealed class ProjectsController(IProjectManagementService projects) : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult<Guid>> CreateProject(CreateProjectRequest request, CancellationToken ct)
    {
        var id = await projects.CreateProjectAsync(new CreateProjectInput(request.OrganizationId, request.Name, request.Key, request.Description), ct);
        return CreatedAtAction(nameof(GetProjectById), new { id }, id);
    }

    [HttpGet]
    public async Task<ActionResult<ProjectPage>> GetProjects(
        [FromQuery] string? search,
        [FromQuery] ProjectStatus? status,
        [FromQuery] string? sortBy,
        CancellationToken ct,
        [FromQuery] bool descending = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25) =>
        Ok(await projects.GetProjectsAsync(new ProjectQuery(search, status, sortBy, descending, page, pageSize), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> GetProjectById(Guid id, CancellationToken ct) =>
        Ok(await projects.GetProjectAsync(id, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProject(Guid id, UpdateProjectRequest request, CancellationToken ct)
    {
        await projects.UpdateProjectAsync(id, new UpdateProjectInput(request.Name, request.Key, request.Description, request.Status), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProject(Guid id, CancellationToken ct)
    {
        await projects.DeleteProjectAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<ProjectMemberDto>>> GetMembers(Guid id, CancellationToken ct) =>
        Ok(await projects.GetMembersAsync(id, ct));

    [HttpPut("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> SetMember(Guid id, Guid userId, ProjectMemberRequest request, CancellationToken ct)
    {
        await projects.SetMemberAsync(id, userId, request.Role, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken ct)
    {
        await projects.RemoveMemberAsync(id, userId, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/teams")]
    public async Task<ActionResult<IReadOnlyList<TeamDto>>> GetTeams(Guid id, CancellationToken ct) =>
        Ok(await projects.GetTeamsAsync(id, ct));

    [HttpPut("{id:guid}/teams/{teamId:guid}")]
    public async Task<IActionResult> AssignTeam(Guid id, Guid teamId, CancellationToken ct)
    {
        await projects.AssignTeamAsync(id, teamId, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/teams/{teamId:guid}")]
    public async Task<IActionResult> RemoveTeam(Guid id, Guid teamId, CancellationToken ct)
    {
        await projects.RemoveTeamAsync(id, teamId, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/github")]
    public async Task<IActionResult> SetGitHubRepository(Guid id, GitHubRepositoryRequest request, CancellationToken ct)
    {
        await projects.SetGitHubRepositoryAsync(id, request.Owner, request.Name, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/github/summary")]
    public async Task<ActionResult<GitHubRepositorySummary>> GetGitHubSummary(Guid id, CancellationToken ct)
    {
        var summary = await projects.GetGitHubSummaryAsync(id, ct);
        return summary is null ? NoContent() : Ok(summary);
    }
}
