using System.Collections.Concurrent;
using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;

namespace DevFlow.IntegrationTests;

public sealed class FakeFileStorageService : IFileStorageService
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new();

    public IReadOnlyCollection<string> Keys => _files.Keys.ToList();

    public async Task<string> UploadAsync(Stream content, string extension, string contentType, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        var key = $"test/{Guid.NewGuid():N}{extension}";
        _files[key] = buffer.ToArray();
        return key;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct) =>
        _files.TryGetValue(storageKey, out var bytes)
            ? Task.FromResult<Stream>(new MemoryStream(bytes))
            : throw new NotFoundException("The attachment file no longer exists.");

    public Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        _files.TryRemove(storageKey, out _);
        return Task.CompletedTask;
    }
}

public sealed class FakeGitHubIntegrationService : IGitHubIntegrationService
{
    public Task<GitHubRepositorySummary?> GetRepositorySummaryAsync(string owner, string name, CancellationToken ct) =>
        Task.FromResult<GitHubRepositorySummary?>(new GitHubRepositorySummary(
            owner,
            name,
            "Test repository",
            3,
            "main",
            $"https://github.com/{owner}/{name}"));
}
