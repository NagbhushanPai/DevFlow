using DevFlow.Application.Common.Authorization;
using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using DevFlow.Domain.Entities;
using DevFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevFlow.Application.Issues;

public sealed record AttachmentDto(
    Guid Id,
    Guid IssueId,
    string FileName,
    string DownloadUrl,
    string ContentType,
    long FileSize,
    Guid UploadedById,
    DateTime CreatedAtUtc);

public sealed record AttachmentDownload(Stream Content, string FileName, string ContentType);

public interface IAttachmentManagementService
{
    Task<IReadOnlyList<AttachmentDto>> GetAttachmentsAsync(Guid issueId, CancellationToken ct);
    Task<AttachmentDto> UploadAsync(Guid issueId, Stream content, string fileName, long fileSize, CancellationToken ct);
    Task<AttachmentDownload> DownloadAsync(Guid attachmentId, CancellationToken ct);
    Task DeleteAsync(Guid attachmentId, CancellationToken ct);
}

/// <summary>
/// Files are stored privately under a random storage key and are only served through the API to project members.
/// The content type is derived from an allow-listed extension, never from the client, so uploads cannot be used
/// to serve active content (HTML/SVG/JS) from our origin.
/// </summary>
public sealed class AttachmentManagementService(
    IApplicationDbContext context,
    IProjectAuthorizationService authorization,
    IFileStorageService fileStorage,
    ILogger<AttachmentManagementService> logger) : IAttachmentManagementService
{
    public const long MaxFileSizeBytes = 25 * 1024 * 1024;
    private const int MaxFileNameLength = 200;

    public static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
            [".pdf"] = "application/pdf",
            [".txt"] = "text/plain",
            [".log"] = "text/plain",
            [".md"] = "text/markdown",
            [".csv"] = "text/csv",
            [".json"] = "application/json",
            [".zip"] = "application/zip",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        };

    public async Task<IReadOnlyList<AttachmentDto>> GetAttachmentsAsync(Guid issueId, CancellationToken ct)
    {
        var issue = await FindIssueAsync(issueId, ct);
        await authorization.RequireRoleAsync(issue.ProjectId, ProjectMemberRole.Viewer, ct);

        var attachments = await context.Attachments
            .Where(attachment => attachment.IssueId == issueId)
            .OrderBy(attachment => attachment.CreatedAtUtc)
            .ToListAsync(ct);

        return attachments.Select(ToDto).ToList();
    }

    public async Task<AttachmentDto> UploadAsync(Guid issueId, Stream content, string fileName, long fileSize, CancellationToken ct)
    {
        var issue = await FindIssueAsync(issueId, ct);
        await authorization.RequireRoleAsync(issue.ProjectId, ProjectMemberRole.Developer, ct);

        if (fileSize <= 0)
        {
            throw new BadRequestException("A non-empty file is required.");
        }

        if (fileSize > MaxFileSizeBytes)
        {
            throw new BadRequestException($"The file exceeds the maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        var safeName = SanitizeFileName(fileName);
        var extension = Path.GetExtension(safeName);
        if (!AllowedContentTypes.TryGetValue(extension, out var contentType))
        {
            throw new BadRequestException($"Files of type '{extension}' are not allowed. Allowed: {string.Join(", ", AllowedContentTypes.Keys)}.");
        }

        var storageKey = await fileStorage.UploadAsync(content, extension, contentType, ct);
        var attachment = new Attachment
        {
            IssueId = issueId,
            FileName = safeName,
            StorageKey = storageKey,
            ContentType = contentType,
            FileSize = fileSize,
            UploadedById = authorization.CurrentUserId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        context.Attachments.Add(attachment);

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch
        {
            await TryDeleteBlobAsync(storageKey);
            throw;
        }

        return ToDto(attachment);
    }

    public async Task<AttachmentDownload> DownloadAsync(Guid attachmentId, CancellationToken ct)
    {
        var attachment = await FindAttachmentAsync(attachmentId, ct);
        var issue = await FindIssueAsync(attachment.IssueId, ct);
        await authorization.RequireRoleAsync(issue.ProjectId, ProjectMemberRole.Viewer, ct);

        var stream = await fileStorage.OpenReadAsync(attachment.StorageKey, ct);
        return new AttachmentDownload(stream, attachment.FileName, attachment.ContentType);
    }

    public async Task DeleteAsync(Guid attachmentId, CancellationToken ct)
    {
        var attachment = await FindAttachmentAsync(attachmentId, ct);
        var issue = await FindIssueAsync(attachment.IssueId, ct);
        var role = await authorization.RequireRoleAsync(issue.ProjectId, ProjectMemberRole.Developer, ct);

        if (role != ProjectMemberRole.Manager && attachment.UploadedById != authorization.CurrentUserId)
        {
            throw new ForbiddenAccessException("Only the uploader or a project manager can delete this attachment.");
        }

        // Remove the database row first: a leftover blob is harmless (and logged), a row pointing at a
        // missing blob is a user-visible error.
        context.Attachments.Remove(attachment);
        await context.SaveChangesAsync(ct);
        await TryDeleteBlobAsync(attachment.StorageKey);
    }

    public static string SanitizeFileName(string? fileName)
    {
        // Normalise Windows separators so path stripping behaves the same on every OS.
        var name = Path.GetFileName((fileName ?? string.Empty).Replace('\\', '/')).Trim();
        char[] invalid = [.. Path.GetInvalidFileNameChars(), '<', '>', ':', '"', '|', '?', '*'];
        var cleaned = new string(name.Select(c => invalid.Contains(c) || char.IsControl(c) ? '_' : c).ToArray()).Trim('.', ' ');

        if (string.IsNullOrEmpty(cleaned))
        {
            throw new BadRequestException("A valid file name is required.");
        }

        if (cleaned.Length > MaxFileNameLength)
        {
            var extension = Path.GetExtension(cleaned);
            cleaned = cleaned[..(MaxFileNameLength - extension.Length)] + extension;
        }

        return cleaned;
    }

    private async Task TryDeleteBlobAsync(string storageKey)
    {
        try
        {
            await fileStorage.DeleteAsync(storageKey, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete stored file {StorageKey}; it is now orphaned.", storageKey);
        }
    }

    private async Task<Issue> FindIssueAsync(Guid issueId, CancellationToken ct) =>
        await context.Issues.SingleOrDefaultAsync(issue => issue.Id == issueId && !issue.IsDeleted, ct)
        ?? throw new NotFoundException("Issue was not found.");

    private async Task<Attachment> FindAttachmentAsync(Guid attachmentId, CancellationToken ct) =>
        await context.Attachments.SingleOrDefaultAsync(attachment => attachment.Id == attachmentId, ct)
        ?? throw new NotFoundException("Attachment was not found.");

    private static AttachmentDto ToDto(Attachment a) =>
        new(a.Id, a.IssueId, a.FileName, $"/api/attachments/{a.Id}/download", a.ContentType, a.FileSize, a.UploadedById, a.CreatedAtUtc);
}
