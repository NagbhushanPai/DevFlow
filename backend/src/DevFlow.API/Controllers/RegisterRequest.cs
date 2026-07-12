namespace DevFlow.API.Controllers;

public sealed record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password);