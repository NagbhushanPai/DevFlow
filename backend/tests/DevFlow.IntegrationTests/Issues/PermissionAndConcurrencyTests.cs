using System.Net;
using System.Net.Http.Json;
using DevFlow.Domain.Entities;
using DevFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static DevFlow.IntegrationTests.TestApi;

namespace DevFlow.IntegrationTests.Issues;

public sealed class PermissionAndConcurrencyTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const int Manager = 1;
    private const int Developer = 2;
    private const int Viewer = 3;

    [Fact]
    public async Task Viewer_CanReadButCannotWrite()
    {
        var owner = await RegisterAsync(factory, "perm-owner");
        var viewer = await RegisterAsync(factory, "perm-viewer");
        var (organizationId, projectId) = await CreateProjectAsync(owner.Client);
        await AddProjectMemberAsync(owner.Client, organizationId, projectId, viewer.UserId, Viewer);
        var issue = await CreateIssueAsync(owner.Client, projectId);

        Assert.Equal(HttpStatusCode.OK, (await viewer.Client.GetAsync($"/api/projects/{projectId}/issues")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.Client.PostAsJsonAsync($"/api/projects/{projectId}/issues",
            new { Title = "Nope", Type = 1, Priority = 2 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.Client.PostAsJsonAsync($"/api/issues/{issue.Id}/comments", new { Content = "hi" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.Client.DeleteAsync($"/api/issues/{issue.Id}")).StatusCode);

        var project = await viewer.Client.GetFromJsonAsync<ProjectResponse>($"/api/projects/{projectId}");
        Assert.Equal(Viewer, project!.MyRole);
    }

    [Fact]
    public async Task Developer_CanDeleteOwnIssue_ButNotOthers_AndCannotManageSprints()
    {
        var owner = await RegisterAsync(factory, "perm-mgr");
        var developer = await RegisterAsync(factory, "perm-dev");
        var (organizationId, projectId) = await CreateProjectAsync(owner.Client);
        await AddProjectMemberAsync(owner.Client, organizationId, projectId, developer.UserId, Developer);

        var managersIssue = await CreateIssueAsync(owner.Client, projectId);
        var developersIssue = await CreateIssueAsync(developer.Client, projectId);

        Assert.Equal(HttpStatusCode.Forbidden, (await developer.Client.DeleteAsync($"/api/issues/{managersIssue.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await developer.Client.DeleteAsync($"/api/issues/{developersIssue.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await developer.Client.PostAsJsonAsync($"/api/projects/{projectId}/sprints",
            new { Name = "Sprint X" })).StatusCode);

        // Managers can delete anyone's issue.
        var anotherDeveloperIssue = await CreateIssueAsync(developer.Client, projectId);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.Client.DeleteAsync($"/api/issues/{anotherDeveloperIssue.Id}")).StatusCode);
    }

    [Fact]
    public async Task Outsider_CannotSeeProject()
    {
        var owner = await RegisterAsync(factory, "perm-o");
        var outsider = await RegisterAsync(factory, "perm-x");
        var (_, projectId) = await CreateProjectAsync(owner.Client);

        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.Client.GetAsync($"/api/projects/{projectId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.Client.GetAsync($"/api/projects/{projectId}/issues")).StatusCode);
    }

    [Fact]
    public async Task ConcurrentIssueCreation_ProducesUniqueSequentialNumbers()
    {
        var owner = await RegisterAsync(factory, "concurrency");
        var (_, projectId) = await CreateProjectAsync(owner.Client);

        var issues = await Task.WhenAll(Enumerable.Range(0, 10).Select(i => CreateIssueAsync(owner.Client, projectId, $"Issue {i}")));

        var numbers = issues.Select(issue => issue.IssueNumber).Order().ToList();
        Assert.Equal(Enumerable.Range(1, 10), numbers);
    }

    [Fact]
    public async Task ProjectUpdate_IsVisibleToOtherMembersImmediately()
    {
        var owner = await RegisterAsync(factory, "cache-owner");
        var developer = await RegisterAsync(factory, "cache-dev");
        var (organizationId, projectId) = await CreateProjectAsync(owner.Client);
        await AddProjectMemberAsync(owner.Client, organizationId, projectId, developer.UserId, Developer);

        var before = await developer.Client.GetFromJsonAsync<ProjectResponse>($"/api/projects/{projectId}"); // primes the cache
        (await owner.Client.PutAsJsonAsync($"/api/projects/{projectId}", new { Name = "Renamed", before!.Key, Description = "", Status = 2 })).EnsureSuccessStatusCode();
        var after = await developer.Client.GetFromJsonAsync<ProjectResponse>($"/api/projects/{projectId}");

        Assert.Equal("Renamed", after!.Name);
        Assert.Equal(Developer, after.MyRole);
    }

    [Fact]
    public async Task DuplicateProjectKey_ReturnsConflict()
    {
        var owner = await RegisterAsync(factory, "dupkey");
        var organizationId = await CreateOrganizationAsync(owner.Client);
        var key = "D" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        (await owner.Client.PostAsJsonAsync("/api/projects", new { OrganizationId = organizationId, Name = "One", Key = key })).EnsureSuccessStatusCode();
        var second = await owner.Client.PostAsJsonAsync("/api/projects", new { OrganizationId = organizationId, Name = "Two", Key = key });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task WriteRequests_AreAudited_WithTheAuthenticatedUser()
    {
        var owner = await RegisterAsync(factory, "audit");
        var organizationId = await CreateOrganizationAsync(owner.Client);
        var path = "/api/organizations";

        AuditLog? entry = null;
        for (var attempt = 0; attempt < 50 && entry is null; attempt++)
        {
            await Task.Delay(100);
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            entry = await db.AuditLogs.FirstOrDefaultAsync(log => log.UserId == owner.UserId && log.Path == path);
        }

        Assert.NotNull(entry);
        Assert.Equal("POST", entry.HttpMethod);
        Assert.Equal(201, entry.StatusCode);
        Assert.NotEqual(Guid.Empty, organizationId);
    }

    [Fact]
    public async Task Timestamps_AreSerializedAsUtc()
    {
        var owner = await RegisterAsync(factory, "utc");
        var (_, projectId) = await CreateProjectAsync(owner.Client);
        var issue = await CreateIssueAsync(owner.Client, projectId);

        var json = await owner.Client.GetStringAsync($"/api/issues/{issue.Id}");
        var createdAt = System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("createdAtUtc").GetString();

        // Without the "Z" browsers interpret the value as local time and show it hours off.
        Assert.EndsWith("Z", createdAt);
    }

    private sealed record ProjectResponse(Guid Id, string Name, string Key, int? MyRole);
}
