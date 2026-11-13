using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace DevFlow.Infrastructure.Storage;

/// <summary>
/// Stores attachments in a <b>private</b> Azure Blob container (works against Azurite locally).
/// Files are only reachable through the API, which checks project membership.
/// </summary>
public sealed class AzureBlobStorageService : IFileStorageService
{
    private readonly AzureStorageSettings _settings;
    private readonly Lazy<BlobServiceClient> _serviceClient;
    private volatile bool _containerReady;

    public AzureBlobStorageService(IOptions<AzureStorageSettings> options)
    {
        _settings = options.Value;
        _serviceClient = new Lazy<BlobServiceClient>(() =>
        {
            if (string.IsNullOrWhiteSpace(_settings.ConnectionString))
            {
                throw new ExternalServiceException("Attachment storage is not configured (AzureStorage:ConnectionString).");
            }

            var clientOptions = new BlobClientOptions
            {
                Retry = { MaxRetries = 3, Mode = RetryMode.Exponential, Delay = TimeSpan.FromMilliseconds(200) },
            };
            return new BlobServiceClient(_settings.ConnectionString, clientOptions);
        });
    }

    public async Task<string> UploadAsync(Stream content, string extension, string contentType, CancellationToken ct)
    {
        var container = await GetContainerAsync(ct);
        var storageKey = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        try
        {
            await container.GetBlobClient(storageKey).UploadAsync(
                content,
                new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
                ct);
            return storageKey;
        }
        catch (RequestFailedException ex)
        {
            throw new ExternalServiceException("Failed to upload the file to storage.", ex);
        }
    }

    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct)
    {
        var container = await GetContainerAsync(ct);
        try
        {
            return await container.GetBlobClient(storageKey).OpenReadAsync(cancellationToken: ct);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new NotFoundException("The attachment file no longer exists.");
        }
        catch (RequestFailedException ex)
        {
            throw new ExternalServiceException("Failed to read the file from storage.", ex);
        }
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        var container = await GetContainerAsync(ct);
        try
        {
            await container.GetBlobClient(storageKey).DeleteIfExistsAsync(cancellationToken: ct);
        }
        catch (RequestFailedException ex)
        {
            throw new ExternalServiceException("Failed to delete the file from storage.", ex);
        }
    }

    private async Task<BlobContainerClient> GetContainerAsync(CancellationToken ct)
    {
        var container = _serviceClient.Value.GetBlobContainerClient(_settings.ContainerName);
        if (_containerReady)
        {
            return container;
        }

        try
        {
            // PublicAccessType.None: blobs are never anonymously readable.
            await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
            _containerReady = true;
            return container;
        }
        catch (RequestFailedException ex)
        {
            throw new ExternalServiceException("Attachment storage is unavailable.", ex);
        }
    }
}
