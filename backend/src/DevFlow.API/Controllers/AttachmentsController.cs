using DevFlow.Application.Issues;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevFlow.API.Controllers;

[Route("api")]
[Authorize]
public sealed class AttachmentsController(IAttachmentManagementService attachments) : BaseApiController
{
    private const long MaxFileSizeBytes = 25 * 1024 * 1024;

    [HttpGet("issues/{issueId:guid}/attachments")]
    public async Task<ActionResult<IReadOnlyList<AttachmentDto>>> GetAttachments(Guid issueId, CancellationToken ct) =>
        Ok(await attachments.GetAttachmentsAsync(issueId, ct));

    [HttpPost("issues/{issueId:guid}/attachments")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<ActionResult<AttachmentDto>> Upload(Guid issueId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("A non-empty file is required.");
        if (file.Length > MaxFileSizeBytes) return BadRequest("The file exceeds the maximum allowed size.");

        await using var stream = file.OpenReadStream();
        var result = await attachments.UploadAsync(issueId, stream, file.FileName, file.ContentType, file.Length, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("attachments/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await attachments.DeleteAsync(id, ct); return NoContent(); }
}
