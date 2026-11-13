using DevFlow.Application.Sprints;
using Microsoft.AspNetCore.Mvc;

namespace DevFlow.API.Controllers;

[Route("api")]
public sealed class SprintsController(ISprintManagementService sprints) : BaseApiController
{
    [HttpGet("projects/{projectId:guid}/sprints")]
    public async Task<ActionResult<IReadOnlyList<SprintDto>>> Get(Guid projectId, CancellationToken ct) =>
        Ok(await sprints.GetSprintsAsync(projectId, ct));

    [HttpPost("projects/{projectId:guid}/sprints")]
    public async Task<ActionResult<SprintDto>> Create(Guid projectId, SprintRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await sprints.CreateAsync(projectId, ToInput(request), ct));

    [HttpPut("sprints/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SprintRequest request, CancellationToken ct)
    {
        await sprints.UpdateAsync(id, ToInput(request), ct);
        return NoContent();
    }

    [HttpDelete("sprints/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sprints.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("sprints/{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        await sprints.StartAsync(id, ct);
        return NoContent();
    }

    [HttpPost("sprints/{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        await sprints.CompleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("projects/{projectId:guid}/backlog")]
    public async Task<ActionResult<IReadOnlyList<BacklogIssueDto>>> Backlog(Guid projectId, CancellationToken ct) =>
        Ok(await sprints.GetBacklogAsync(projectId, ct));

    [HttpGet("sprints/{id:guid}/issues")]
    public async Task<ActionResult<IReadOnlyList<BacklogIssueDto>>> SprintIssues(Guid id, CancellationToken ct) =>
        Ok(await sprints.GetSprintIssuesAsync(id, ct));

    [HttpPut("sprints/{id:guid}/issues/{issueId:guid}")]
    public async Task<IActionResult> AssignIssue(Guid id, Guid issueId, CancellationToken ct)
    {
        await sprints.AssignIssueAsync(id, issueId, ct);
        return NoContent();
    }

    [HttpDelete("sprints/{id:guid}/issues/{issueId:guid}")]
    public async Task<IActionResult> RemoveIssue(Guid id, Guid issueId, CancellationToken ct)
    {
        await sprints.RemoveIssueAsync(id, issueId, ct);
        return NoContent();
    }

    private static SprintInput ToInput(SprintRequest request) =>
        new(request.Name, request.Goal, request.StartDateUtc, request.EndDateUtc);
}
