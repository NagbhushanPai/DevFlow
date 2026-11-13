using DevFlow.Application.Common.Authorization;
using DevFlow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DevFlow.API.Hubs;

[Authorize]
public sealed class IssueHub(IProjectAuthorizationService authorization) : Hub
{
    public async Task JoinProject(Guid projectId)
    {
        if (await authorization.GetEffectiveRoleAsync(projectId, Context.ConnectionAborted) is not ProjectMemberRole)
        {
            throw new HubException("Project membership is required.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(projectId));
    }

    public Task LeaveProject(Guid projectId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(projectId));

    public static string GroupName(Guid projectId) => $"project-{projectId}";
}
