namespace DevFlow.Infrastructure.GitHub;

public sealed class GitHubSettings
{
    public const string SectionName = "GitHub";

    public string? Token { get; set; }
}
