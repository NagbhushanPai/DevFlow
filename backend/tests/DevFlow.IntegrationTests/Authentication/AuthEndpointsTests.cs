using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace DevFlow.IntegrationTests.Authentication;

public sealed class AuthEndpointsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private sealed record AuthenticationResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyCollection<string> Roles,
    string Token,
    DateTime ExpiresAtUtc);

    [Fact]
    public async Task Register_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new
        {
            FirstName = "Integration",
            LastName = "User",
            Email = $"integration-{Guid.NewGuid()}@devflow.com",
            Password = "Test@123"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }



    [Fact]
public async Task Register_WithValidRequest_ReturnsAuthenticationResult()
{
    // Arrange
    var email =
        $"integration-{Guid.NewGuid()}@devflow.com";

    var request = new
    {
        FirstName = "Integration",
        LastName = "User",
        Email = email,
        Password = "Test@123"
    };

    // Act
    var response = await _client.PostAsJsonAsync(
        "/api/auth/register",
        request);

    var result =
        await response.Content
            .ReadFromJsonAsync<AuthenticationResponse>();

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    Assert.NotNull(result);

    Assert.Equal(email, result.Email);

    Assert.Contains(
        "Developer",
        result.Roles);

    Assert.False(
        string.IsNullOrWhiteSpace(result.Token));
}




[Fact]
public async Task Login_WithValidCredentials_ReturnsOk()
{
    // Arrange
    var email =
        $"login-{Guid.NewGuid()}@devflow.com";

    var password = "Test@123";

    var registerRequest = new
    {
        FirstName = "Login",
        LastName = "User",
        Email = email,
        Password = password
    };

    await _client.PostAsJsonAsync(
        "/api/auth/register",
        registerRequest);

    var loginRequest = new
    {
        Email = email,
        Password = password
    };

    // Act
    var response = await _client.PostAsJsonAsync(
        "/api/auth/login",
        loginRequest);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}



[Fact]
public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
{
    // Arrange
    var email =
        $"invalid-{Guid.NewGuid()}@devflow.com";

    await _client.PostAsJsonAsync(
        "/api/auth/register",
        new
        {
            FirstName = "Invalid",
            LastName = "Login",
            Email = email,
            Password = "Test@123"
        });

    // Act
    var response = await _client.PostAsJsonAsync(
        "/api/auth/login",
        new
        {
            Email = email,
            Password = "WrongPassword"
        });

    // Assert
    Assert.Equal(
        HttpStatusCode.Unauthorized,
        response.StatusCode);
}



[Fact]
public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
{
    // Arrange
    var email =
        $"duplicate-{Guid.NewGuid()}@devflow.com";

    var request = new
    {
        FirstName = "Duplicate",
        LastName = "User",
        Email = email,
        Password = "Test@123"
    };

    await _client.PostAsJsonAsync(
        "/api/auth/register",
        request);

    // Act
    var response = await _client.PostAsJsonAsync(
        "/api/auth/register",
        request);

    // Assert
    Assert.Equal(
        HttpStatusCode.BadRequest,
        response.StatusCode);
}

[Fact]
public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
{
    // Act
    var response = await _client.GetAsync(
        "/api/test/protected");

    // Assert
    Assert.Equal(
        HttpStatusCode.Unauthorized,
        response.StatusCode);
}


[Fact]
public async Task ProtectedEndpoint_WithValidToken_ReturnsOk()
{
    // Arrange
    var email =
        $"protected-{Guid.NewGuid()}@devflow.com";

    var registerResponse =
        await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FirstName = "Protected",
                LastName = "User",
                Email = email,
                Password = "Test@123"
            });

    var authentication =
        await registerResponse.Content
            .ReadFromJsonAsync<AuthenticationResponse>();

    Assert.NotNull(authentication);

    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(
            "Bearer",
            authentication.Token);

    // Act
    var response = await _client.GetAsync(
        "/api/test/protected");

    // Assert
    Assert.Equal(
        HttpStatusCode.OK,
        response.StatusCode);
}
}
