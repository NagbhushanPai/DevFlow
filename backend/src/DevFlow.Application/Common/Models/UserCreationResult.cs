namespace DevFlow.Application.Common.Models;

public sealed record UserCreationResult(
    bool Succeeded,
    Guid? UserId,
    IEnumerable<string> Errors);