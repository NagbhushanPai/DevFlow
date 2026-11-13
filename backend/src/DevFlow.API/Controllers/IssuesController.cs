using DevFlow.Application.Issues;
using DevFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace DevFlow.API.Controllers;

[Route("api")]
public sealed class IssuesController(IIssueManagementService issues) : BaseApiController
{
    [HttpGet("projects/{projectId:guid}/issues")]
    public async Task<ActionResult<IssuePage>> GetIssues(
        Guid projectId,
        [FromQuery] string? search,
        [FromQuery] IssueStatus? status,
        [FromQuery] IssuePriority? priority,
        [FromQuery] Guid? assigneeId,
        [FromQuery] string? sortBy,
        CancellationToken ct,
        [FromQuery] bool descending = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25) =>
        Ok(await issues.GetIssuesAsync(projectId, new IssueQuery(search, status, priority, assigneeId, sortBy, descending, page, pageSize), ct));

    [HttpPost("projects/{projectId:guid}/issues")]
    public async Task<ActionResult<IssueDto>> Create(Guid projectId, IssueCreateRequest request, CancellationToken ct)
    {
        var input = new IssueInput(request.Title, request.Description, request.Type, IssueStatus.Backlog, request.Priority, request.AssigneeId);
        var issue = await issues.CreateIssueAsync(projectId, input, ct);
        return CreatedAtAction(nameof(Get), new { id = issue.Id }, issue);
    }

    [HttpGet("issues/{id:guid}")]
    public async Task<ActionResult<IssueDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await issues.GetIssueAsync(id, ct));

    [HttpPut("issues/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, IssueUpdateRequest request, CancellationToken ct)
    {
        var input = new IssueInput(request.Title, request.Description, request.Type, request.Status, request.Priority, request.AssigneeId);
        await issues.UpdateIssueAsync(id, input, ct);
        return NoContent();
    }

    [HttpDelete("issues/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await issues.DeleteIssueAsync(id, ct);
        return NoContent();
    }

    [HttpGet("issues/{id:guid}/comments")]
    public async Task<ActionResult<IReadOnlyList<CommentDto>>> Comments(Guid id, CancellationToken ct) =>
        Ok(await issues.GetCommentsAsync(id, ct));

    [HttpPost("issues/{id:guid}/comments")]
    public async Task<ActionResult<CommentDto>> AddComment(Guid id, CommentRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await issues.AddCommentAsync(id, request.Content, ct));

    [HttpDelete("comments/{id:guid}")]
    public async Task<IActionResult> DeleteComment(Guid id, CancellationToken ct)
    {
        await issues.DeleteCommentAsync(id, ct);
        return NoContent();
    }

    [HttpGet("issues/{id:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<HistoryDto>>> History(Guid id, CancellationToken ct) =>
        Ok(await issues.GetHistoryAsync(id, ct));
}
