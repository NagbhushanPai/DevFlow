namespace DevFlow.Application.Common.Interfaces;

public sealed record GitHubRepositorySummary(
    string Owner,
    string Name,
    string? Description,
    int OpenIssuesCount,
    string DefaultBranch,
    string HtmlUrl);

public interface IGitHubIntegrationService
{
    Task<GitHubRepositorySummary?> GetRepositorySummaryAsync(string owner, string name, CancellationToken ct);
}
