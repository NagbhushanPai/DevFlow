using DevFlow.Application.Projects.Commands.CreateProject;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
}