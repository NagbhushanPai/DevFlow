using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace DevFlow.IntegrationTests.Projects;

public sealed class ProjectEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Owner_CanCreateFilterAndAssignTeamToProject()
    {
        var client = factory.CreateClient();
        var authentication = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authentication.Token);

        var organizationResponse = await client.PostAsJsonAsync("/api/organizations", new { Name = "Projects Org", Description = "" });
        var organizationId = await organizationResponse.Content.ReadFromJsonAsync<Guid>();
        Assert.Equal(HttpStatusCode.Created, organizationResponse.StatusCode);

        var projectResponse = await client.PostAsJsonAsync("/api/projects", new { OrganizationId = organizationId, Name = "DevFlow API", Key = "DFA", Description = "" });
        var projectId = await projectResponse.Content.ReadFromJsonAsync<Guid>();
        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);

        var pageResponse = await client.GetAsync("/api/projects?search=API&sortBy=key&page=1&pageSize=10");
        var page = await pageResponse.Content.ReadFromJsonAsync<ProjectPageResponse>();
        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);
        Assert.NotNull(page);
        Assert.Contains(page.Items, item => item.Id == projectId);

        var teamResponse = await client.PostAsJsonAsync($"/api/organizations/{organizationId}/teams", new { Name = "Platform", Description = "" });
        var team = await teamResponse.Content.ReadFromJsonAsync<TeamResponse>();
        Assert.NotNull(team);
        var assignment = await client.PutAsync($"/api/projects/{projectId}/teams/{team.Id}", null);
        Assert.Equal(HttpStatusCode.NoContent, assignment.StatusCode);
    }

    private static async Task<AuthenticationResponse> RegisterAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new { FirstName = "Project", LastName = "Owner", Email = $"project-{Guid.NewGuid()}@devflow.test", Password = "Test@123" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthenticationResponse>())!;
    }

    private sealed record AuthenticationResponse(Guid UserId, string Token);
    private sealed record TeamResponse(Guid Id, Guid OrganizationId, string Name, string? Description);
    private sealed record ProjectItem(Guid Id, Guid OrganizationId, string Name, string Key);
    private sealed record ProjectPageResponse(IReadOnlyList<ProjectItem> Items, int Page, int PageSize, int TotalCount);
}
