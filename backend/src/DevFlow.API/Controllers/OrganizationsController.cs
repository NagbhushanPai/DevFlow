using DevFlow.Application.Organizations;
using DevFlow.Application.Organizations.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace DevFlow.API.Controllers;

[Route("api/organizations")]
public sealed class OrganizationsController(IOrganizationManagementService organizations) : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateOrganizationRequest request, CancellationToken ct)
    {
        var id = await organizations.CreateOrganizationAsync(request.Name, request.Description, ct);
        return CreatedAtAction(nameof(GetById), new { organizationId = id }, id);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrganizationDto>>> Get(CancellationToken ct) =>
        Ok(await organizations.GetMyOrganizationsAsync(ct));

    [HttpGet("{organizationId:guid}")]
    public async Task<ActionResult<OrganizationDto>> GetById(Guid organizationId, CancellationToken ct) =>
        Ok(await organizations.GetOrganizationAsync(organizationId, ct));

    [HttpPut("{organizationId:guid}")]
    public async Task<IActionResult> Update(Guid organizationId, UpdateOrganizationRequest request, CancellationToken ct)
    {
        await organizations.UpdateOrganizationAsync(organizationId, request.Name, request.Description, ct);
        return NoContent();
    }

    [HttpDelete("{organizationId:guid}")]
    public async Task<IActionResult> Delete(Guid organizationId, CancellationToken ct)
    {
        await organizations.DeleteOrganizationAsync(organizationId, ct);
        return NoContent();
    }

    [HttpGet("{organizationId:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<OrganizationMemberDto>>> GetMembers(Guid organizationId, CancellationToken ct) =>
        Ok(await organizations.GetMembersAsync(organizationId, ct));

    [HttpPut("{organizationId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> AddMember(Guid organizationId, Guid userId, OrganizationMemberRequest request, CancellationToken ct)
    {
        await organizations.AddMemberAsync(organizationId, userId, request.Role, ct);
        return NoContent();
    }

    [HttpDelete("{organizationId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid organizationId, Guid userId, CancellationToken ct)
    {
        await organizations.RemoveMemberAsync(organizationId, userId, ct);
        return NoContent();
    }

    [HttpGet("{organizationId:guid}/teams")]
    public async Task<ActionResult<IReadOnlyList<TeamDto>>> GetTeams(Guid organizationId, CancellationToken ct) =>
        Ok(await organizations.GetTeamsAsync(organizationId, ct));

    [HttpPost("{organizationId:guid}/teams")]
    public async Task<ActionResult<TeamDto>> CreateTeam(Guid organizationId, CreateTeamRequest request, CancellationToken ct)
    {
        var team = await organizations.CreateTeamAsync(organizationId, request.Name, request.Description, ct);
        return CreatedAtAction(nameof(TeamsController.GetById), "Teams", new { teamId = team.Id }, team);
    }
}
