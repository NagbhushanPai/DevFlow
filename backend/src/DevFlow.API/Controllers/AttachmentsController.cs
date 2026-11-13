using DevFlow.Application.Issues;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace DevFlow.API.Controllers;

[Route("api")]
public sealed class AttachmentsController(IAttachmentManagementService attachments) : BaseApiController
{
    [HttpGet("issues/{issueId:guid}/attachments")]
    public async Task<ActionResult<IReadOnlyList<AttachmentDto>>> GetAttachments(Guid issueId, CancellationToken ct) =>
        Ok(await attachments.GetAttachmentsAsync(issueId, ct));

    [HttpPost("issues/{issueId:guid}/attachments")]
    [RequestSizeLimit(AttachmentManagementService.MaxFileSizeBytes + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = AttachmentManagementService.MaxFileSizeBytes + 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AttachmentDto>> Upload(Guid issueId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("A non-empty file is required.");
        }

        await using var stream = file.OpenReadStream();
        var result = await attachments.UploadAsync(issueId, stream, file.FileName, file.Length, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Streams the file to project members. Always sent as a download with <c>nosniff</c> so the browser never
    /// renders uploaded content inline from our origin.
    /// </summary>
    [HttpGet("attachments/{id:guid}/download")]
    [Produces("application/octet-stream")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var download = await attachments.DownloadAsync(id, ct);
        Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        Response.Headers[HeaderNames.ContentSecurityPolicy] = "default-src 'none'; sandbox";
        return File(download.Content, download.ContentType, download.FileName);
    }

    [HttpDelete("attachments/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await attachments.DeleteAsync(id, ct);
        return NoContent();
    }
}
