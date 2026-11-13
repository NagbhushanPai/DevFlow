using DevFlow.Application.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DevFlow.API.Hubs;

[Authorize]
public sealed class IssueHub(IProjectAuthorizationService authorization) : Hub
{
    /// <summary>Subscribes the connection to a project's events. Only project members (or org managers) may join.</summary>
    public async Task JoinProject(Guid projectId)
    {
        // Use the connection's user rather than IHttpContextAccessor, which is not reliable inside hubs.
        if (!Guid.TryParse(Context.UserIdentifier, out var userId))
        {
            throw new HubException("Unauthorized.");
        }

        if (await authorization.GetEffectiveRoleAsync(projectId, userId, Context.ConnectionAborted) is null)
        {
            throw new HubException("Project membership is required.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(projectId));
    }

    public Task LeaveProject(Guid projectId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(projectId));

    public static string GroupName(Guid projectId) => $"project-{projectId}";
}
