using DevFlow.Application.Common.Interfaces;

namespace DevFlow.IntegrationTests;

public sealed class FakeFileStorageService : IFileStorageService
{
    private readonly Dictionary<string, byte[]> _files = [];

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        var url = $"https://storage.test/{Guid.NewGuid():N}/{fileName}";
        _files[url] = buffer.ToArray();
        return url;
    }

    public Task DeleteAsync(string fileUrl, CancellationToken ct)
    {
        _files.Remove(fileUrl);
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
