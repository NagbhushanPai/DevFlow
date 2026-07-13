using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Common.Models;
using DevFlow.Application.Features.Authentication.Commands.Login;
using Moq;

namespace DevFlow.UnitTests.Application.Authentication;

public sealed class LoginCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _identityServiceMock = new Mock<IIdentityService>();
        _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        _handler = new LoginCommandHandler(
            _identityServiceMock.Object,
            _jwtTokenGeneratorMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreValid_ReturnsAuthenticationResult()
    {
        // Arrange
        var user = new UserInfo(
            Guid.NewGuid(),
            "Test",
            "Developer",
            "test@devflow.com",
            ["Developer"]);

        var command = new LoginCommand(
            user.Email,
            "Test@123");

        _identityServiceMock
            .Setup(service =>
                service.GetUserByEmailAsync(
                    command.Email,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _identityServiceMock
            .Setup(service =>
                service.CheckPasswordAsync(
                    user.Id,
                    command.Password,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _jwtTokenGeneratorMock
            .Setup(generator =>
                generator.GenerateToken(user))
            .Returns(
                new JwtTokenResult(
                    "test-token",
                    DateTime.UtcNow.AddHours(1)));

        // Act
        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.Email, result.Email);
        Assert.Contains("Developer", result.Roles);
        Assert.Equal("test-token", result.Token);
    }


    [Fact]
public async Task Handle_WhenUserDoesNotExist_ThrowsInvalidCredentialsException()
{
    // Arrange
    var command = new LoginCommand(
        "unknown@devflow.com",
        "Test@123");

    _identityServiceMock
        .Setup(service =>
            service.GetUserByEmailAsync(
                command.Email,
                It.IsAny<CancellationToken>()))
        .ReturnsAsync((UserInfo?)null);

    // Act
    var action = async () =>
        await _handler.Handle(
            command,
            CancellationToken.None);

    // Assert
    await Assert.ThrowsAsync<
        DevFlow.Application.Common.Exceptions.InvalidCredentialsException>(
            action);
}




[Fact]
public async Task Handle_WhenPasswordIsInvalid_ThrowsInvalidCredentialsException()
{
    // Arrange
    var user = new UserInfo(
        Guid.NewGuid(),
        "Test",
        "Developer",
        "test@devflow.com",
        ["Developer"]);

    var command = new LoginCommand(
        user.Email,
        "WrongPassword");

    _identityServiceMock
        .Setup(service =>
            service.GetUserByEmailAsync(
                command.Email,
                It.IsAny<CancellationToken>()))
        .ReturnsAsync(user);

    _identityServiceMock
        .Setup(service =>
            service.CheckPasswordAsync(
                user.Id,
                command.Password,
                It.IsAny<CancellationToken>()))
        .ReturnsAsync(false);

    // Act
    var action = async () =>
        await _handler.Handle(
            command,
            CancellationToken.None);

    // Assert
    await Assert.ThrowsAsync<
        DevFlow.Application.Common.Exceptions.InvalidCredentialsException>(
            action);
}
}