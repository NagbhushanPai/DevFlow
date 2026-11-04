using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace DevFlow.IntegrationTests.Integrations;

public sealed class AttachmentAndGitHubEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task ProjectMember_CanUploadListAndDeleteAttachment()
    {
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var projectId = await CreateProjectAsync(client);
        var issueId = await CreateIssueAsync(client, projectId);

        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent("attachment content"u8.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(file, "file", "notes.txt");

        var upload = await client.PostAsync($"/api/issues/{issueId}/attachments", content);
        var attachment = await upload.Content.ReadFromJsonAsync<AttachmentResponse>();

        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        Assert.NotNull(attachment);
        Assert.Equal("notes.txt", attachment.FileName);

        var list = await client.GetFromJsonAsync<List<AttachmentResponse>>($"/api/issues/{issueId}/attachments");
        Assert.Single(list!);

        var delete = await client.DeleteAsync($"/api/attachments/{attachment.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Empty(await client.GetFromJsonAsync<List<AttachmentResponse>>($"/api/issues/{issueId}/attachments") ?? []);
    }

    [Fact]
    public async Task ProjectMember_CanSaveAndReadGitHubRepositorySummary()
    {
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var projectId = await CreateProjectAsync(client);

        var save = await client.PutAsJsonAsync($"/api/projects/{projectId}/github", new { Owner = "octo", Name = "devflow" });
        var summary = await client.GetFromJsonAsync<GitHubSummaryResponse>($"/api/projects/{projectId}/github/summary");

        Assert.Equal(HttpStatusCode.NoContent, save.StatusCode);
        Assert.NotNull(summary);
        Assert.Equal("octo", summary.Owner);
        Assert.Equal("devflow", summary.Name);
        Assert.Equal(3, summary.OpenIssuesCount);
    }

    private static async Task<AuthResponse> RegisterAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            FirstName = "Integration",
            LastName = "Tester",
            Email = $"integration-{Guid.NewGuid()}@devflow.test",
            Password = "Test@123"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private static async Task<Guid> CreateProjectAsync(HttpClient client)
    {
        var organization = await (await client.PostAsJsonAsync("/api/organizations", new
        {
            Name = $"Integration Org {Guid.NewGuid():N}",
            Description = ""
        })).Content.ReadFromJsonAsync<Guid>();
        var response = await client.PostAsJsonAsync("/api/projects", new
        {
            OrganizationId = organization,
            Name = "Integration Project",
            Key = $"IT{Random.Shared.Next(100, 999)}",
            Description = ""
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Guid>())!;
    }

    private static async Task<Guid> CreateIssueAsync(HttpClient client, Guid projectId)
    {
        var response = await client.PostAsJsonAsync($"/api/projects/{projectId}/issues", new
        {
            Title = "Attachment issue",
            Description = "Test",
            Type = 1,
            Priority = 2,
            AssigneeId = (Guid?)null
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IssueResponse>())!.Id;
    }

    private sealed record AuthResponse(Guid UserId, string Token);
    private sealed record IssueResponse(Guid Id);
    private sealed record AttachmentResponse(Guid Id, string FileName, string FileUrl);
    private sealed record GitHubSummaryResponse(string Owner, string Name, int OpenIssuesCount);
}
