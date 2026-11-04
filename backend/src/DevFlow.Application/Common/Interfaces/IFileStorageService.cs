namespace DevFlow.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct);

    Task DeleteAsync(string fileUrl, CancellationToken ct);
}
