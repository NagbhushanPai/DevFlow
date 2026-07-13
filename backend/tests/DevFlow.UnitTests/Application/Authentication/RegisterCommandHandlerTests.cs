using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Common.Models;
using DevFlow.Application.Features.Authentication.Commands.Register;
using Moq;

namespace DevFlow.UnitTests.Application.Authentication;

public sealed class RegisterCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _identityServiceMock = new Mock<IIdentityService>();
        _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        _handler = new RegisterCommandHandler(
            _identityServiceMock.Object,
            _jwtTokenGeneratorMock.Object);
    }

    [Fact]
    public async Task Handle_WhenRegistrationIsValid_ReturnsAuthenticationResult()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var command = new RegisterCommand(
            "Test",
            "Developer",
            "test@devflow.com",
            "Test@123");

        _identityServiceMock
            .Setup(service =>
                service.UserExistsByEmailAsync(
                    command.Email,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _identityServiceMock
            .Setup(service =>
                service.CreateUserAsync(
                    command.FirstName,
                    command.LastName,
                    command.Email,
                    command.Password,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new UserCreationResult(
                    true,
                    userId,
                    []));

        _identityServiceMock
            .Setup(service =>
                service.AddToRoleAsync(
                    userId,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _jwtTokenGeneratorMock
            .Setup(generator =>
                generator.GenerateToken(
                    It.IsAny<UserInfo>()))
            .Returns(
                new JwtTokenResult(
                    "test-token",
                    DateTime.UtcNow.AddHours(1)));

        // Act
        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.Equal(userId, result.UserId);
        Assert.Equal(command.FirstName, result.FirstName);
        Assert.Equal(command.LastName, result.LastName);
        Assert.Equal(command.Email, result.Email);
        Assert.Contains("Developer", result.Roles);
        Assert.Equal("test-token", result.Token);
    }

    [Fact]
public async Task Handle_WhenEmailAlreadyExists_ThrowsRegistrationException()
{
    // Arrange
    var command = new RegisterCommand(
        "Test",
        "Developer",
        "test@devflow.com",
        "Test@123");

    _identityServiceMock
        .Setup(service =>
            service.UserExistsByEmailAsync(
                command.Email,
                It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);

    // Act
    var action = async () =>
        await _handler.Handle(
            command,
            CancellationToken.None);

    // Assert
    await Assert.ThrowsAsync<
        DevFlow.Application.Common.Exceptions.RegistrationException>(
            action);
}


[Fact]
public async Task Handle_WhenIdentityCreationFails_ThrowsRegistrationException()
{
    // Arrange
    var command = new RegisterCommand(
        "Test",
        "Developer",
        "test@devflow.com",
        "weak");

    _identityServiceMock
        .Setup(service =>
            service.UserExistsByEmailAsync(
                command.Email,
                It.IsAny<CancellationToken>()))
        .ReturnsAsync(false);

    _identityServiceMock
        .Setup(service =>
            service.CreateUserAsync(
                command.FirstName,
                command.LastName,
                command.Email,
                command.Password,
                It.IsAny<CancellationToken>()))
        .ReturnsAsync(
            new UserCreationResult(
                false,
                null,
                ["Password validation failed."]));

    // Act
    var action = async () =>
        await _handler.Handle(
            command,
            CancellationToken.None);

    // Assert
    await Assert.ThrowsAsync<
        DevFlow.Application.Common.Exceptions.RegistrationException>(
            action);
}
}