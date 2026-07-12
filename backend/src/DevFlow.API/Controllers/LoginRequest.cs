namespace DevFlow.API.Controllers;

public sealed record LoginRequest(
    string Email,
    string Password);