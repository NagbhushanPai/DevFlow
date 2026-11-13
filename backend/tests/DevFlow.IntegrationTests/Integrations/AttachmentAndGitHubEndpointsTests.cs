using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using static DevFlow.IntegrationTests.TestApi;

namespace DevFlow.IntegrationTests.Integrations;

public sealed class AttachmentAndGitHubEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task ProjectMember_CanUploadDownloadListAndDeleteAttachment()
    {
        var owner = await RegisterAsync(factory, "attach");
        var (_, projectId) = await CreateProjectAsync(owner.Client);
        var issue = await CreateIssueAsync(owner.Client, projectId);

        var upload = await UploadAsync(owner.Client, issue.Id, "notes.txt", "attachment content"u8.ToArray(), "text/html");
        var attachment = await upload.Content.ReadFromJsonAsync<AttachmentResponse>();

        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        Assert.NotNull(attachment);
        Assert.Equal("notes.txt", attachment.FileName);
        Assert.Equal("text/plain", attachment.ContentType); // derived from the extension, not the client header
        Assert.Equal($"/api/attachments/{attachment.Id}/download", attachment.DownloadUrl);

        var download = await owner.Client.GetAsync(attachment.DownloadUrl);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("attachment content", await download.Content.ReadAsStringAsync());
        Assert.Equal("attachment", download.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("nosniff", download.Headers.GetValues("X-Content-Type-Options").Single());

        Assert.Single((await owner.Client.GetFromJsonAsync<List<AttachmentResponse>>($"/api/issues/{issue.Id}/attachments"))!);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.Client.DeleteAsync($"/api/attachments/{attachment.Id}")).StatusCode);
        Assert.Empty((await owner.Client.GetFromJsonAsync<List<AttachmentResponse>>($"/api/issues/{issue.Id}/attachments"))!);
    }

    [Theory]
    [InlineData("page.html")]
    [InlineData("image.svg")]
    [InlineData("script.js")]
    public async Task Upload_RejectsActiveContentTypes(string fileName)
    {
        var owner = await RegisterAsync(factory, "attach-bad");
        var (_, projectId) = await CreateProjectAsync(owner.Client);
        var issue = await CreateIssueAsync(owner.Client, projectId);

        var upload = await UploadAsync(owner.Client, issue.Id, fileName, "<script>alert(1)</script>"u8.ToArray(), "text/plain");

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
    }

    [Fact]
    public async Task NonMember_CannotDownloadAttachment()
    {
        var owner = await RegisterAsync(factory, "attach-owner");
        var outsider = await RegisterAsync(factory, "attach-outsider");
        var (_, projectId) = await CreateProjectAsync(owner.Client);
        var issue = await CreateIssueAsync(owner.Client, projectId);
        var attachment = await (await UploadAsync(owner.Client, issue.Id, "a.txt", "secret"u8.ToArray(), "text/plain"))
            .Content.ReadFromJsonAsync<AttachmentResponse>();

        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.Client.GetAsync(attachment!.DownloadUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync(attachment.DownloadUrl)).StatusCode);
    }

    [Fact]
    public async Task ProjectManager_CanSaveAndReadGitHubRepositorySummary()
    {
        var owner = await RegisterAsync(factory, "github");
        var (_, projectId) = await CreateProjectAsync(owner.Client);

        var save = await owner.Client.PutAsJsonAsync($"/api/projects/{projectId}/github", new { Owner = "octo", Name = "devflow" });
        var summary = await owner.Client.GetFromJsonAsync<GitHubSummaryResponse>($"/api/projects/{projectId}/github/summary");

        Assert.Equal(HttpStatusCode.NoContent, save.StatusCode);
        Assert.NotNull(summary);
        Assert.Equal("octo", summary.Owner);
        Assert.Equal(3, summary.OpenIssuesCount);
    }

    [Fact]
    public async Task GitHubRepository_RejectsInvalidNames()
    {
        var owner = await RegisterAsync(factory, "github-bad");
        var (_, projectId) = await CreateProjectAsync(owner.Client);

        var save = await owner.Client.PutAsJsonAsync($"/api/projects/{projectId}/github", new { Owner = "../octo", Name = "devflow" });

        Assert.Equal(HttpStatusCode.BadRequest, save.StatusCode);
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, Guid issueId, string fileName, byte[] bytes, string contentType)
    {
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, "file", fileName);
        return await client.PostAsync($"/api/issues/{issueId}/attachments", content);
    }

    private sealed record AttachmentResponse(Guid Id, string FileName, string DownloadUrl, string ContentType);

    private sealed record GitHubSummaryResponse(string Owner, string Name, int OpenIssuesCount);
}
