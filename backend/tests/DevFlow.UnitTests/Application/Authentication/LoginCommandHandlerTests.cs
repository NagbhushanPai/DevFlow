using DevFlow.Application.Authentication.Commands.Login;
using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Common.Models;
using Moq;

namespace DevFlow.UnitTests.Application.Authentication;

public sealed class LoginCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identity = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly Mock<IRefreshTokenService> _refreshTokens = new();
    private readonly LoginCommandHandler _handler;
    private readonly UserInfo _user = new(Guid.NewGuid(), "Test", "Developer", "test@devflow.com", ["Developer"]);

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(_identity.Object, _jwt.Object, _refreshTokens.Object);
        _identity.Setup(s => s.GetUserByEmailAsync(_user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreValid_ReturnsAccessAndRefreshTokens()
    {
        _identity.Setup(s => s.CheckPasswordAsync(_user.Id, "Test@1234", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _jwt.Setup(g => g.GenerateToken(_user)).Returns(new JwtTokenResult("access", DateTime.UtcNow.AddMinutes(15)));
        _refreshTokens.Setup(r => r.IssueAsync(_user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenResult("refresh", DateTime.UtcNow.AddDays(7)));

        var result = await _handler.Handle(new LoginCommand(_user.Email, "Test@1234"), CancellationToken.None);

        Assert.Equal(_user.Id, result.UserId);
        Assert.Equal("access", result.Token);
        Assert.Equal("refresh", result.RefreshToken);
        Assert.Contains("Developer", result.Roles);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ThrowsInvalidCredentials()
    {
        _identity.Setup(s => s.GetUserByEmailAsync("unknown@devflow.com", It.IsAny<CancellationToken>())).ReturnsAsync((UserInfo?)null);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _handler.Handle(new LoginCommand("unknown@devflow.com", "Test@1234"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenPasswordIsInvalidOrLockedOut_ThrowsInvalidCredentialsAndIssuesNoTokens()
    {
        _identity.Setup(s => s.CheckPasswordAsync(_user.Id, "WrongPassword", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _handler.Handle(new LoginCommand(_user.Email, "WrongPassword"), CancellationToken.None));
        _refreshTokens.Verify(r => r.IssueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
