using DevFlow.Application.Organizations;
using DevFlow.Application.Organizations.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevFlow.API.Controllers;

[ApiController]
[Route("api/teams")]
[Authorize]
public sealed class TeamsController(IOrganizationManagementService organizationService) : ControllerBase
{
    [HttpGet("{teamId:guid}")]
    public async Task<ActionResult<TeamDto>> GetById(Guid teamId, CancellationToken cancellationToken) =>
        Ok(await organizationService.GetTeamAsync(teamId, cancellationToken));

    [HttpPut("{teamId:guid}")]
    public async Task<IActionResult> Update(Guid teamId, UpdateTeamRequest request, CancellationToken cancellationToken)
    {
        await organizationService.UpdateTeamAsync(teamId, request.Name, request.Description, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{teamId:guid}")]
    public async Task<IActionResult> Delete(Guid teamId, CancellationToken cancellationToken)
    {
        await organizationService.DeleteTeamAsync(teamId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{teamId:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<TeamMemberDto>>> GetMembers(Guid teamId, CancellationToken cancellationToken) =>
        Ok(await organizationService.GetTeamMembersAsync(teamId, cancellationToken));

    [HttpPut("{teamId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> AddMember(Guid teamId, Guid userId, TeamMemberRequest request, CancellationToken cancellationToken)
    {
        await organizationService.AddTeamMemberAsync(teamId, userId, request.Role, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{teamId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid teamId, Guid userId, CancellationToken cancellationToken)
    {
        await organizationService.RemoveTeamMemberAsync(teamId, userId, cancellationToken);
        return NoContent();
    }
}

public sealed record UpdateTeamRequest(string Name, string? Description);
public sealed record TeamMemberRequest(string Role);
