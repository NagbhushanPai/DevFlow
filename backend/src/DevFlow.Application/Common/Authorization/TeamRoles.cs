namespace DevFlow.Application.Common.Authorization;

public static class TeamRoles
{
    public const string Lead = "Lead";
    public const string Developer = "Developer";
    public const string Viewer = "Viewer";

    public static bool IsValid(string role) =>
        role is Lead or Developer or Viewer;
}
