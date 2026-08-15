using DevFlow.Application.Projects.Commands.CreateProject;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using DevFlow.Application.Projects.DTOs;
using DevFlow.Application.Projects.Queries.GetProjects;
using DevFlow.Application.Projects.Queries.GetProjectById;
using DevFlow.Application.Projects;
using DevFlow.Domain.Enums;

namespace DevFlow.API.Controllers;

[Route("api/projects")]
[Authorize]
public sealed class ProjectsController : BaseApiController
{
    private readonly IProjectManagementService _projectService;

    public ProjectsController(IProjectManagementService projectService)
    {
        _projectService = projectService;
    }
    [HttpPost]
    public async Task<ActionResult<Guid>> CreateProject(
        CreateProjectCommand command,
        CancellationToken cancellationToken)
    {
        var projectId = await Sender.Send(
            command,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            projectId);
    }

    [HttpGet]
    public async Task<ActionResult<ProjectPage>> GetProjects(
        [FromQuery] string? search,
        [FromQuery] ProjectStatus? status,
        [FromQuery] string? sortBy,
        CancellationToken cancellationToken,
        [FromQuery] bool descending = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var projects = await _projectService.GetProjectsAsync(search, status, sortBy, descending, page, pageSize, cancellationToken);

        return Ok(projects);
    }


    [HttpGet("{id:guid}")]
public async Task<ActionResult<ProjectDto>> GetProjectById(
    Guid id,
    CancellationToken cancellationToken)
{
    var project = await _projectService.GetProjectAsync(id, cancellationToken);

    return Ok(project);
}

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProject(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        await _projectService.UpdateProjectAsync(id, request.Name, request.Key, request.Description, request.Status, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProject(Guid id, CancellationToken cancellationToken)
    {
        await _projectService.DeleteProjectAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<ProjectMemberDto>>> GetMembers(Guid id, CancellationToken cancellationToken) =>
        Ok(await _projectService.GetMembersAsync(id, cancellationToken));

    [HttpPut("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> SetMember(Guid id, Guid userId, ProjectMemberRequest request, CancellationToken cancellationToken)
    {
        await _projectService.SetMemberAsync(id, userId, request.Role, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        await _projectService.RemoveMemberAsync(id, userId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/teams")]
    public async Task<ActionResult<IReadOnlyList<DevFlow.Application.Organizations.DTOs.TeamDto>>> GetTeams(Guid id, CancellationToken cancellationToken) =>
        Ok(await _projectService.GetTeamsAsync(id, cancellationToken));

    [HttpPut("{id:guid}/teams/{teamId:guid}")]
    public async Task<IActionResult> AssignTeam(Guid id, Guid teamId, CancellationToken cancellationToken)
    {
        await _projectService.AssignTeamAsync(id, teamId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/teams/{teamId:guid}")]
    public async Task<IActionResult> RemoveTeam(Guid id, Guid teamId, CancellationToken cancellationToken)
    {
        await _projectService.RemoveTeamAsync(id, teamId, cancellationToken);
        return NoContent();
    }
}

public sealed record UpdateProjectRequest(string Name, string Key, string? Description, ProjectStatus Status);
public sealed record ProjectMemberRequest(ProjectMemberRole Role);
