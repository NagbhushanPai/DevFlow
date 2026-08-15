using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace DevFlow.IntegrationTests.Organizations;

public sealed class OrganizationAndTeamEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Owner_CanManageOrganizationAndTeam_WhileMemberCannotManage()
    {
        var owner = await CreateAuthorizedClientAsync("owner");
        var member = await CreateAuthorizedClientAsync("member");

        var createOrganization = await owner.Client.PostAsJsonAsync("/api/organizations", new { Name = "DevFlow", Description = "Platform team" });
        Assert.Equal(HttpStatusCode.Created, createOrganization.StatusCode);
        var organizationId = await createOrganization.Content.ReadFromJsonAsync<Guid>();

        var addMember = await owner.Client.PutAsJsonAsync($"/api/organizations/{organizationId}/members/{member.UserId}", new { Role = "Member" });
        Assert.Equal(HttpStatusCode.NoContent, addMember.StatusCode);

        var createTeam = await owner.Client.PostAsJsonAsync($"/api/organizations/{organizationId}/teams", new { Name = "Backend", Description = "Backend delivery team" });
        Assert.Equal(HttpStatusCode.Created, createTeam.StatusCode);
        var team = await createTeam.Content.ReadFromJsonAsync<TeamResponse>();
        Assert.NotNull(team);

        var addTeamMember = await owner.Client.PutAsJsonAsync($"/api/teams/{team.Id}/members/{member.UserId}", new { Role = "Developer" });
        Assert.Equal(HttpStatusCode.NoContent, addTeamMember.StatusCode);

        var members = await member.Client.GetAsync($"/api/teams/{team.Id}/members");
        Assert.Equal(HttpStatusCode.OK, members.StatusCode);

        var forbiddenUpdate = await member.Client.PutAsJsonAsync($"/api/organizations/{organizationId}", new { Name = "Changed", Description = "" });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenUpdate.StatusCode);

        var update = await owner.Client.PutAsJsonAsync($"/api/teams/{team.Id}", new { Name = "Platform", Description = "Updated" });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
    }

    private async Task<AuthorizedClient> CreateAuthorizedClientAsync(string prefix)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            FirstName = prefix,
            LastName = "User",
            Email = $"{prefix}-{Guid.NewGuid()}@devflow.test",
            Password = "Test@123"
        });
        response.EnsureSuccessStatusCode();
        var authentication = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(authentication);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authentication.Token);
        return new AuthorizedClient(client, authentication.UserId);
    }

    private sealed record AuthorizedClient(HttpClient Client, Guid UserId);

    private sealed record AuthenticationResponse(Guid UserId, string Token);
    private sealed record TeamResponse(Guid Id, Guid OrganizationId, string Name, string? Description);
}
