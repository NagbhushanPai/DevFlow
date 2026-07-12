namespace DevFlow.Application.Common.Models;

public sealed record UserInfo(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyCollection<string> Roles);