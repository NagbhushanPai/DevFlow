using DevFlow.Application.Organizations;
using DevFlow.Application.Organizations.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace DevFlow.API.Controllers;

[Route("api/teams")]
public sealed class TeamsController(IOrganizationManagementService organizations) : BaseApiController
{
    [HttpGet("{teamId:guid}")]
    public async Task<ActionResult<TeamDto>> GetById(Guid teamId, CancellationToken ct) =>
        Ok(await organizations.GetTeamAsync(teamId, ct));

    [HttpPut("{teamId:guid}")]
    public async Task<IActionResult> Update(Guid teamId, UpdateTeamRequest request, CancellationToken ct)
    {
        await organizations.UpdateTeamAsync(teamId, request.Name, request.Description, ct);
        return NoContent();
    }

    [HttpDelete("{teamId:guid}")]
    public async Task<IActionResult> Delete(Guid teamId, CancellationToken ct)
    {
        await organizations.DeleteTeamAsync(teamId, ct);
        return NoContent();
    }

    [HttpGet("{teamId:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<TeamMemberDto>>> GetMembers(Guid teamId, CancellationToken ct) =>
        Ok(await organizations.GetTeamMembersAsync(teamId, ct));

    [HttpPut("{teamId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> AddMember(Guid teamId, Guid userId, TeamMemberRequest request, CancellationToken ct)
    {
        await organizations.AddTeamMemberAsync(teamId, userId, request.Role, ct);
        return NoContent();
    }

    [HttpDelete("{teamId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid teamId, Guid userId, CancellationToken ct)
    {
        await organizations.RemoveTeamMemberAsync(teamId, userId, ct);
        return NoContent();
    }
}
