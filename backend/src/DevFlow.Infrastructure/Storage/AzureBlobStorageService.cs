using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace DevFlow.Infrastructure.Storage;

public sealed class AzureBlobStorageService(IOptions<AzureStorageSettings> options) : IFileStorageService
{
    private readonly AzureStorageSettings _settings = options.Value;

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct)
    {
        var container = await GetContainerAsync(ct);
        var blobName = $"{Guid.NewGuid()}-{fileName}";
        var blobClient = container.GetBlobClient(blobName);

        try
        {
            await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);
            return blobClient.Uri.ToString();
        }
        catch (RequestFailedException ex)
        {
            throw new ExternalServiceException("Failed to upload the file to Azure Blob Storage.", ex);
        }
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken ct)
    {
        var container = await GetContainerAsync(ct);
        var blobName = new Uri(fileUrl).Segments[^1];

        try
        {
            await container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: ct);
        }
        catch (RequestFailedException ex)
        {
            throw new ExternalServiceException("Failed to delete the file from Azure Blob Storage.", ex);
        }
    }

    private async Task<BlobContainerClient> GetContainerAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.ConnectionString))
        {
            throw new ExternalServiceException("Azure Blob Storage is not configured.");
        }

        var clientOptions = new BlobClientOptions
        {
            Retry =
            {
                MaxRetries = 3,
                Mode = RetryMode.Exponential,
                Delay = TimeSpan.FromMilliseconds(200)
            }
        };

        try
        {
            var serviceClient = new BlobServiceClient(_settings.ConnectionString, clientOptions);
            var container = serviceClient.GetBlobContainerClient(_settings.ContainerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);
            return container;
        }
        catch (RequestFailedException ex)
        {
            throw new ExternalServiceException("Azure Blob Storage is unavailable.", ex);
        }
    }
}
