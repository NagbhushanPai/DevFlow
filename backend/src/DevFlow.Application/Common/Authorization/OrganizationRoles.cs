namespace DevFlow.Application.Common.Authorization;

public static class OrganizationRoles
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Member = "Member";

    public static bool IsValid(string role) =>
        role is Owner or Admin or Member;

    public static bool CanManage(string role) =>
        role is Owner or Admin;
}
