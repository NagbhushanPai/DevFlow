using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace DevFlow.IntegrationTests;

/// <summary>Helpers shared by integration tests to create users and data through the public API.</summary>
public static class TestApi
{
    public const string Password = "Test@1234";

    public sealed record AuthResponse(Guid UserId, string FirstName, string LastName, string Email, IReadOnlyCollection<string> Roles, string Token, DateTime ExpiresAtUtc);

    public sealed record IssueResponse(Guid Id, Guid ProjectId, int IssueNumber, string Title, Guid? SprintId);

    public sealed record AuthorizedClient(HttpClient Client, Guid UserId, string Email);

    public static async Task<AuthorizedClient> RegisterAsync(CustomWebApplicationFactory factory, string prefix = "user")
    {
        var client = factory.CreateClient();
        var email = $"{prefix}-{Guid.NewGuid():N}@devflow.test";
        var response = await client.PostAsJsonAsync("/api/auth/register", new { FirstName = prefix, LastName = "Tester", Email = email, Password });
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return new AuthorizedClient(client, auth.UserId, email);
    }

    public static async Task<Guid> CreateOrganizationAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/organizations", new { Name = $"Org {Guid.NewGuid():N}", Description = "" });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    public static async Task<(Guid OrganizationId, Guid ProjectId)> CreateProjectAsync(HttpClient client)
    {
        var organizationId = await CreateOrganizationAsync(client);
        var key = "P" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var response = await client.PostAsJsonAsync("/api/projects", new { OrganizationId = organizationId, Name = "Test Project", Key = key, Description = "" });
        response.EnsureSuccessStatusCode();
        return (organizationId, await response.Content.ReadFromJsonAsync<Guid>());
    }

    public static async Task<IssueResponse> CreateIssueAsync(HttpClient client, Guid projectId, string title = "Test issue")
    {
        var response = await client.PostAsJsonAsync($"/api/projects/{projectId}/issues", new { Title = title, Description = "", Type = 1, Priority = 2, AssigneeId = (Guid?)null });
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Create issue failed with {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }

        return (await response.Content.ReadFromJsonAsync<IssueResponse>())!;
    }

    /// <summary>Adds <paramref name="member"/> to the organization and project with the given project role (1=Manager, 2=Developer, 3=Viewer).</summary>
    public static async Task AddProjectMemberAsync(HttpClient owner, Guid organizationId, Guid projectId, Guid member, int projectRole)
    {
        (await owner.PutAsJsonAsync($"/api/organizations/{organizationId}/members/{member}", new { Role = "Member" })).EnsureSuccessStatusCode();
        (await owner.PutAsJsonAsync($"/api/projects/{projectId}/members/{member}", new { Role = projectRole })).EnsureSuccessStatusCode();
    }
}
