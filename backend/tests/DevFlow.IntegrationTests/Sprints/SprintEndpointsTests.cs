using System.Net;
using System.Net.Http.Json;
using static DevFlow.IntegrationTests.TestApi;

namespace DevFlow.IntegrationTests.Sprints;

public sealed class SprintEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Manager_CanRunSprintLifecycle_AndIssueCountIsAccurate()
    {
        var owner = await RegisterAsync(factory, "sprint");
        var (_, projectId) = await CreateProjectAsync(owner.Client);
        var first = await CreateIssueAsync(owner.Client, projectId, "First");
        var second = await CreateIssueAsync(owner.Client, projectId, "Second");
        await CreateIssueAsync(owner.Client, projectId, "Stays in backlog");

        var create = await owner.Client.PostAsJsonAsync($"/api/projects/{projectId}/sprints", new { Name = "Sprint 1", Goal = "Ship" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var sprint = (await create.Content.ReadFromJsonAsync<SprintResponse>())!;

        (await owner.Client.PutAsync($"/api/sprints/{sprint.Id}/issues/{first.Id}", null)).EnsureSuccessStatusCode();
        (await owner.Client.PutAsync($"/api/sprints/{sprint.Id}/issues/{second.Id}", null)).EnsureSuccessStatusCode();

        var sprints = await owner.Client.GetFromJsonAsync<List<SprintResponse>>($"/api/projects/{projectId}/sprints");
        Assert.Equal(2, Assert.Single(sprints!).IssueCount);

        var backlog = await owner.Client.GetFromJsonAsync<List<BacklogIssue>>($"/api/projects/{projectId}/backlog");
        Assert.Single(backlog!);
        Assert.Equal(2, (await owner.Client.GetFromJsonAsync<List<BacklogIssue>>($"/api/sprints/{sprint.Id}/issues"))!.Count);

        Assert.Equal(HttpStatusCode.NoContent, (await owner.Client.PostAsync($"/api/sprints/{sprint.Id}/start", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.Client.DeleteAsync($"/api/sprints/{sprint.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.Client.PostAsync($"/api/sprints/{sprint.Id}/complete", null)).StatusCode);

        // Deleting a sprint returns its issues to the backlog.
        Assert.Equal(HttpStatusCode.NoContent, (await owner.Client.DeleteAsync($"/api/sprints/{sprint.Id}")).StatusCode);
        Assert.Equal(3, (await owner.Client.GetFromJsonAsync<List<BacklogIssue>>($"/api/projects/{projectId}/backlog"))!.Count);
    }

    [Fact]
    public async Task CreateSprint_WithEndBeforeStart_ReturnsBadRequest()
    {
        var owner = await RegisterAsync(factory, "sprint-dates");
        var (_, projectId) = await CreateProjectAsync(owner.Client);

        var response = await owner.Client.PostAsJsonAsync($"/api/projects/{projectId}/sprints", new
        {
            Name = "Backwards",
            StartDateUtc = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record SprintResponse(Guid Id, string Name, int Status, int IssueCount);

    private sealed record BacklogIssue(Guid Id, int IssueNumber, string Title, Guid? SprintId);
}
