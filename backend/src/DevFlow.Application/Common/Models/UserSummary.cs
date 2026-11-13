namespace DevFlow.Application.Common.Models;

public sealed record UserSummary(Guid Id, string FirstName, string LastName, string Email)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}
