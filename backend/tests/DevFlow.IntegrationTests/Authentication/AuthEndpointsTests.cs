using System.Net;
using System.Net.Http.Json;
using static DevFlow.IntegrationTests.TestApi;

namespace DevFlow.IntegrationTests.Authentication;

public sealed class AuthEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Register_ReturnsAccessTokenInBody_AndRefreshTokenOnlyAsHttpOnlyCookie()
    {
        var client = factory.CreateClient();
        var email = $"register-{Guid.NewGuid():N}@devflow.test";

        var response = await client.PostAsJsonAsync("/api/auth/register", new { FirstName = "Reg", LastName = "User", Email = email, Password });
        var body = await response.Content.ReadAsStringAsync();
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
        Assert.Contains("Developer", result.Roles);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.DoesNotContain("refreshToken", body, StringComparison.OrdinalIgnoreCase);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("devflow_refresh="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ReturnsValidationErrors()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { FirstName = "Weak", LastName = "User", Email = $"weak-{Guid.NewGuid():N}@devflow.test", Password = "password" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("uppercase", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        var user = await RegisterAsync(factory, "dup");
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { FirstName = "Dup", LastName = "User", Email = user.Email, Password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        var user = await RegisterAsync(factory, "login");
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { user.Email, Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var user = await RegisterAsync(factory, "invalid");
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { user.Email, Password = "WrongPassword1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_IsLockedOutAfterRepeatedFailures_EvenWithCorrectPassword()
    {
        var user = await RegisterAsync(factory, "lockout");
        var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/api/auth/login", new { user.Email, Password = "WrongPassword1!" });
        }

        var response = await client.PostAsJsonAsync("/api/auth/login", new { user.Email, Password });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_RotatesCookie_AndRejectsReuseOfOldToken()
    {
        var client = factory.CreateClient(new() { HandleCookies = false });
        var email = $"refresh-{Guid.NewGuid():N}@devflow.test";
        var register = await client.PostAsJsonAsync("/api/auth/register", new { FirstName = "R", LastName = "U", Email = email, Password });
        var firstCookie = RefreshCookie(register);

        var refresh = await PostWithCookieAsync(client, "/api/auth/refresh", firstCookie);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var refreshed = await refresh.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(refreshed!.Token));
        var secondCookie = RefreshCookie(refresh);
        Assert.NotEqual(firstCookie, secondCookie);

        // Replaying the rotated token is treated as theft: it fails and revokes the whole session family.
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostWithCookieAsync(client, "/api/auth/refresh", firstCookie)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostWithCookieAsync(client, "/api/auth/refresh", secondCookie)).StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var client = factory.CreateClient(new() { HandleCookies = false });
        var register = await client.PostAsJsonAsync("/api/auth/register",
            new { FirstName = "L", LastName = "O", Email = $"logout-{Guid.NewGuid():N}@devflow.test", Password });
        var cookie = RefreshCookie(register);

        Assert.Equal(HttpStatusCode.NoContent, (await PostWithCookieAsync(client, "/api/auth/logout", cookie)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostWithCookieAsync(client, "/api/auth/refresh", cookie)).StatusCode);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized() =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().PostAsync("/api/auth/refresh", null)).StatusCode);

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized() =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/users/me")).StatusCode);

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ReturnsCurrentUser()
    {
        var user = await RegisterAsync(factory, "me");
        var me = await user.Client.GetFromJsonAsync<MeResponse>("/api/users/me");

        Assert.Equal(user.UserId, me!.Id);
        Assert.Equal(user.Email, me.Email);
    }

    private sealed record MeResponse(Guid Id, string Email);

    private static string RefreshCookie(HttpResponseMessage response)
    {
        var header = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("devflow_refresh="));
        return header[..header.IndexOf(';')];
    }

    private static Task<HttpResponseMessage> PostWithCookieAsync(HttpClient client, string url, string cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Cookie", cookie);
        request.Headers.Authorization = null;
        return client.SendAsync(request);
    }
}
