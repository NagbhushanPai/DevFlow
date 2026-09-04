using DevFlow.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DevFlow.API.Hubs;

[Authorize]
public sealed class IssueHub(IApplicationDbContext context, ICurrentUserService currentUser) : Hub
{
    public async Task JoinProject(Guid projectId)
    {
        if (!await HasAccessAsync(projectId)) throw new HubException("Project membership is required.");
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(projectId));
    }

    public Task LeaveProject(Guid projectId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(projectId));

    public static string GroupName(Guid projectId) => $"project-{projectId}";

    private Task<bool> HasAccessAsync(Guid projectId)
    {
        var userId = currentUser.UserId ?? throw new HubException("Unauthorized.");
        return context.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == userId);
    }
}
