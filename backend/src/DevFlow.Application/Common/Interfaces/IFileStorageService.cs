namespace DevFlow.Application.Common.Interfaces;

/// <summary>
/// Private file storage. Callers work with opaque storage keys, never public URLs.
/// </summary>
public interface IFileStorageService
{
    /// <summary>Stores the content under a new random key (ending in <paramref name="extension"/>) and returns the key.</summary>
    Task<string> UploadAsync(Stream content, string extension, string contentType, CancellationToken ct);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct);

    Task DeleteAsync(string storageKey, CancellationToken ct);
}
