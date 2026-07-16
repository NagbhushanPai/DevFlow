using DevFlow.Application.Projects.Commands.CreateProject;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using DevFlow.Application.Projects.DTOs;
using DevFlow.Application.Projects.Queries.GetProjects;

namespace DevFlow.API.Controllers;

[Route("api/projects")]
[Authorize]
public sealed class ProjectsController : BaseApiController
{
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
    public async Task<ActionResult<IReadOnlyList<ProjectSummaryDto>>> GetProjects(
        CancellationToken cancellationToken)
    {
        var projects = await Sender.Send(
            new GetProjectsQuery(),
            cancellationToken);

        return Ok(projects);
    }
}