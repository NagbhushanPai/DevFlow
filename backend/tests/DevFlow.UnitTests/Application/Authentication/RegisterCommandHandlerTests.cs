using DevFlow.Application.Authentication.Commands.Register;
using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Common.Models;
using Moq;

namespace DevFlow.UnitTests.Application.Authentication;

public sealed class RegisterCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identity = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly Mock<IRefreshTokenService> _refreshTokens = new();
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _handler = new RegisterCommandHandler(_identity.Object, _jwt.Object, _refreshTokens.Object);
    }

    [Fact]
    public async Task Handle_WhenRegistrationIsValid_ReturnsTokensAndAssignsDeveloperRole()
    {
        var userId = Guid.NewGuid();
        var command = new RegisterCommand("Test", "Developer", "test@devflow.com", "Test@1234");

        _identity.Setup(s => s.UserExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _identity.Setup(s => s.CreateUserAsync(command.FirstName, command.LastName, command.Email, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserCreationResult(true, userId, []));
        _jwt.Setup(g => g.GenerateToken(It.IsAny<UserInfo>())).Returns(new JwtTokenResult("access", DateTime.UtcNow.AddMinutes(15)));
        _refreshTokens.Setup(r => r.IssueAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenResult("refresh", DateTime.UtcNow.AddDays(7)));

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(userId, result.UserId);
        Assert.Equal(command.Email, result.Email);
        Assert.Contains("Developer", result.Roles);
        Assert.Equal("access", result.Token);
        Assert.Equal("refresh", result.RefreshToken);
        _identity.Verify(s => s.AddToRoleAsync(userId, "Developer", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ThrowsRegistrationException()
    {
        var command = new RegisterCommand("Test", "Developer", "test@devflow.com", "Test@1234");
        _identity.Setup(s => s.UserExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<RegistrationException>(() => _handler.Handle(command, CancellationToken.None));
        _refreshTokens.Verify(r => r.IssueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenIdentityCreationFails_ThrowsRegistrationException()
    {
        var command = new RegisterCommand("Test", "Developer", "test@devflow.com", "weak");
        _identity.Setup(s => s.UserExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _identity.Setup(s => s.CreateUserAsync(command.FirstName, command.LastName, command.Email, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserCreationResult(false, null, ["Password validation failed."]));

        await Assert.ThrowsAsync<RegistrationException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
