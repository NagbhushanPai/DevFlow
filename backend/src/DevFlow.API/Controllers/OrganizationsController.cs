using DevFlow.Application.Organizations.Commands.CreateOrganization;
using DevFlow.Application.Organizations.Queries.GetOrganizations;
using DevFlow.Application.Organizations;
using DevFlow.Application.Organizations.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevFlow.API.Controllers;

[ApiController]
[Route("api/organizations")]
[Authorize]
public sealed class OrganizationsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IOrganizationManagementService _organizationService;

    public OrganizationsController(
        ISender sender,
        IOrganizationManagementService organizationService)
    {
        _sender = sender;
        _organizationService = organizationService;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateOrganizationCommand command, CancellationToken cancellationToken)
    {
        var id = await _sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, id);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<object>>> Get(CancellationToken cancellationToken)
    {
        var orgs = await _sender.Send(new GetOrganizationsQuery(), cancellationToken);
        return Ok(orgs);
    }

    [HttpGet("{organizationId:guid}")]
    public async Task<ActionResult<OrganizationDto>> GetById(Guid organizationId, CancellationToken cancellationToken) =>
        Ok(await _organizationService.GetOrganizationAsync(organizationId, cancellationToken));

    [HttpPut("{organizationId:guid}")]
    public async Task<IActionResult> Update(Guid organizationId, UpdateOrganizationRequest request, CancellationToken cancellationToken)
    {
        await _organizationService.UpdateOrganizationAsync(organizationId, request.Name, request.Description, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{organizationId:guid}")]
    public async Task<IActionResult> Delete(Guid organizationId, CancellationToken cancellationToken)
    {
        await _organizationService.DeleteOrganizationAsync(organizationId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{organizationId:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<OrganizationMemberDto>>> GetMembers(Guid organizationId, CancellationToken cancellationToken) =>
        Ok(await _organizationService.GetMembersAsync(organizationId, cancellationToken));

    [HttpPut("{organizationId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> AddMember(Guid organizationId, Guid userId, OrganizationMemberRequest request, CancellationToken cancellationToken)
    {
        await _organizationService.AddMemberAsync(organizationId, userId, request.Role, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{organizationId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        await _organizationService.RemoveMemberAsync(organizationId, userId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{organizationId:guid}/teams")]
    public async Task<ActionResult<IReadOnlyList<TeamDto>>> GetTeams(Guid organizationId, CancellationToken cancellationToken) =>
        Ok(await _organizationService.GetTeamsAsync(organizationId, cancellationToken));

    [HttpPost("{organizationId:guid}/teams")]
    public async Task<ActionResult<TeamDto>> CreateTeam(Guid organizationId, CreateTeamRequest request, CancellationToken cancellationToken)
    {
        var team = await _organizationService.CreateTeamAsync(organizationId, request.Name, request.Description, cancellationToken);
        return CreatedAtAction(nameof(TeamsController.GetById), "Teams", new { teamId = team.Id }, team);
    }
}

public sealed record UpdateOrganizationRequest(string Name, string? Description);
public sealed record OrganizationMemberRequest(string Role);
public sealed record CreateTeamRequest(string Name, string? Description);
