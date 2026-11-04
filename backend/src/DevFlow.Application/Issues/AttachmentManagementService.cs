using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using DevFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevFlow.Application.Issues;

public sealed record AttachmentDto(Guid Id, Guid IssueId, string FileName, string FileUrl, string ContentType, long FileSize, Guid UploadedById, DateTime CreatedAtUtc);

public interface IAttachmentManagementService
{
    Task<IReadOnlyList<AttachmentDto>> GetAttachmentsAsync(Guid issueId, CancellationToken ct);
    Task<AttachmentDto> UploadAsync(Guid issueId, Stream content, string fileName, string contentType, long fileSize, CancellationToken ct);
    Task DeleteAsync(Guid attachmentId, CancellationToken ct);
}

public sealed class AttachmentManagementService(IApplicationDbContext context, ICurrentUserService currentUser, IFileStorageService fileStorage) : IAttachmentManagementService
{
    public async Task<IReadOnlyList<AttachmentDto>> GetAttachmentsAsync(Guid issueId, CancellationToken ct)
    {
        await AccessIssueAsync(issueId, ct);
        return await context.Attachments.Where(a => a.IssueId == issueId).OrderBy(a => a.CreatedAtUtc).Select(a => ToDto(a)).ToListAsync(ct);
    }

    public async Task<AttachmentDto> UploadAsync(Guid issueId, Stream content, string fileName, string contentType, long fileSize, CancellationToken ct)
    {
        await AccessIssueAsync(issueId, ct);
        var fileUrl = await fileStorage.UploadAsync(content, fileName, contentType, ct);
        var attachment = new Attachment { IssueId = issueId, FileName = fileName, FileUrl = fileUrl, ContentType = contentType, FileSize = fileSize, UploadedById = UserId, CreatedAtUtc = DateTime.UtcNow };
        context.Attachments.Add(attachment);
        await context.SaveChangesAsync(ct);
        return ToDto(attachment);
    }

    public async Task DeleteAsync(Guid attachmentId, CancellationToken ct)
    {
        var attachment = await context.Attachments.SingleOrDefaultAsync(a => a.Id == attachmentId, ct) ?? throw new NotFoundException("Attachment was not found.");
        await AccessIssueAsync(attachment.IssueId, ct);
        await fileStorage.DeleteAsync(attachment.FileUrl, ct);
        context.Attachments.Remove(attachment);
        await context.SaveChangesAsync(ct);
    }

    private Guid UserId => currentUser.UserId ?? throw new UnauthorizedAccessException();
    private async Task AccessIssueAsync(Guid issueId, CancellationToken ct)
    {
        var issue = await context.Issues.SingleOrDefaultAsync(i => i.Id == issueId && !i.IsDeleted, ct) ?? throw new NotFoundException("Issue was not found.");
        if (!await context.ProjectMembers.AnyAsync(m => m.ProjectId == issue.ProjectId && m.UserId == UserId, ct)) throw new ForbiddenAccessException("Project membership is required.");
    }
    private static AttachmentDto ToDto(Attachment a) => new(a.Id, a.IssueId, a.FileName, a.FileUrl, a.ContentType, a.FileSize, a.UploadedById, a.CreatedAtUtc);
}
