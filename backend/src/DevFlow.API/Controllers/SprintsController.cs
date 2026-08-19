using DevFlow.Application.Sprints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevFlow.API.Controllers;

[ApiController, Authorize]
[Route("api")]
public sealed class SprintsController(ISprintManagementService sprints) : ControllerBase
{
    [HttpGet("projects/{projectId:guid}/sprints")] public async Task<ActionResult<IReadOnlyList<SprintDto>>> Get(Guid projectId, CancellationToken ct) => Ok(await sprints.GetSprintsAsync(projectId, ct));
    [HttpPost("projects/{projectId:guid}/sprints")] public async Task<ActionResult<SprintDto>> Create(Guid projectId, SprintRequest request, CancellationToken ct) => StatusCode(201, await sprints.CreateAsync(projectId, request.Name, request.Goal, request.StartDateUtc, request.EndDateUtc, ct));
    [HttpPut("sprints/{id:guid}")] public async Task<IActionResult> Update(Guid id, SprintRequest request, CancellationToken ct) { await sprints.UpdateAsync(id, request.Name, request.Goal, request.StartDateUtc, request.EndDateUtc, ct); return NoContent(); }
    [HttpDelete("sprints/{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await sprints.DeleteAsync(id, ct); return NoContent(); }
    [HttpPost("sprints/{id:guid}/start")] public async Task<IActionResult> Start(Guid id, CancellationToken ct) { await sprints.StartAsync(id, ct); return NoContent(); }
    [HttpPost("sprints/{id:guid}/complete")] public async Task<IActionResult> Complete(Guid id, CancellationToken ct) { await sprints.CompleteAsync(id, ct); return NoContent(); }
    [HttpGet("projects/{projectId:guid}/backlog")] public async Task<ActionResult<IReadOnlyList<IssueDto>>> Backlog(Guid projectId, CancellationToken ct) => Ok(await sprints.GetBacklogAsync(projectId, ct));
    [HttpPut("sprints/{id:guid}/issues/{issueId:guid}")] public async Task<IActionResult> AssignIssue(Guid id, Guid issueId, CancellationToken ct) { await sprints.AssignIssueAsync(id, issueId, ct); return NoContent(); }
}
public sealed record SprintRequest(string Name, string? Goal, DateTime? StartDateUtc, DateTime? EndDateUtc);
