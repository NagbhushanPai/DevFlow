using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevFlow.Infrastructure.GitHub;

public sealed class GitHubIntegrationService(HttpClient httpClient, IOptions<GitHubSettings> options, ILogger<GitHubIntegrationService> logger) : IGitHubIntegrationService
{
    public async Task<GitHubRepositorySummary?> GetRepositorySummaryAsync(string owner, string name, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"repos/{owner}/{name}");
            if (!string.IsNullOrWhiteSpace(options.Value.Token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.Token);
            }

            using var response = await httpClient.SendAsync(request, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;

            response.EnsureSuccessStatusCode();

            var repository = await response.Content.ReadFromJsonAsync<GitHubRepositoryResponse>(cancellationToken: ct)
                ?? throw new ExternalServiceException("GitHub returned an empty response.");

            return new GitHubRepositorySummary(owner, name, repository.Description, repository.OpenIssuesCount, repository.DefaultBranch, repository.HtmlUrl);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "GitHub repository lookup failed for {Owner}/{Name}.", owner, name);
            throw new ExternalServiceException($"GitHub is unavailable or the repository '{owner}/{name}' could not be reached.", ex);
        }
    }

    private sealed record GitHubRepositoryResponse(
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("open_issues_count")] int OpenIssuesCount,
        [property: JsonPropertyName("default_branch")] string DefaultBranch,
        [property: JsonPropertyName("html_url")] string HtmlUrl);
}
