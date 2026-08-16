using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace DevFlow.IntegrationTests.Issues;

public sealed class IssueEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Member_CanManageIssueCommentsAndHistory()
    {
        var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var organization = await (await client.PostAsJsonAsync("/api/organizations", new { Name = "Issues Org", Description = "" })).Content.ReadFromJsonAsync<Guid>();
        var projectResponse = await client.PostAsJsonAsync("/api/projects", new { OrganizationId = organization, Name = "Issue Project", Key = "ISS", Description = "" });
        var projectId = await projectResponse.Content.ReadFromJsonAsync<Guid>();

        var create = await client.PostAsJsonAsync($"/api/projects/{projectId}/issues", new { Title = "First issue", Description = "Details", Type = 1, Priority = 3, AssigneeId = (Guid?)null });
        var issue = await create.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.NotNull(issue);

        var comment = await client.PostAsJsonAsync($"/api/issues/{issue!.Id}/comments", new { Content = "Investigating" });
        Assert.Equal(HttpStatusCode.Created, comment.StatusCode);
        var update = await client.PutAsJsonAsync($"/api/issues/{issue.Id}", new { Title = "Updated issue", Description = "Details", Type = 1, Status = 3, Priority = 4, AssigneeId = (Guid?)null });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        var history = await client.GetFromJsonAsync<List<HistoryResponse>>($"/api/issues/{issue.Id}/history");
        Assert.NotEmpty(history!);

        var page = await client.GetFromJsonAsync<IssuePageResponse>($"/api/projects/{projectId}/issues?search=Updated&status=3&page=1&pageSize=10");
        Assert.Single(page!.Items);
        var delete = await client.DeleteAsync($"/api/issues/{issue.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    private static async Task<AuthResponse> RegisterAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new { FirstName = "Issue", LastName = "Tester", Email = $"issue-{Guid.NewGuid()}@devflow.test", Password = "Test@123" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private sealed record AuthResponse(Guid UserId, string Token);
    private sealed record IssueResponse(Guid Id, Guid ProjectId, int IssueNumber, string Title);
    private sealed record HistoryResponse(Guid Id, string FieldName);
    private sealed record IssuePageResponse(IReadOnlyList<IssueResponse> Items, int Page, int PageSize, int TotalCount);
}
