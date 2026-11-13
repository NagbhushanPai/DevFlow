using DevFlow.Application.Common;
using DevFlow.Application.Common.Authorization;
using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Organizations.DTOs;
using DevFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevFlow.Application.Organizations;

public interface IOrganizationManagementService
{
    Task<Guid> CreateOrganizationAsync(string name, string? description, CancellationToken ct);
    Task<IReadOnlyList<OrganizationDto>> GetMyOrganizationsAsync(CancellationToken ct);
    Task<OrganizationDto> GetOrganizationAsync(Guid organizationId, CancellationToken ct);
    Task UpdateOrganizationAsync(Guid organizationId, string name, string? description, CancellationToken ct);
    Task DeleteOrganizationAsync(Guid organizationId, CancellationToken ct);
    Task<IReadOnlyList<OrganizationMemberDto>> GetMembersAsync(Guid organizationId, CancellationToken ct);
    Task AddMemberAsync(Guid organizationId, Guid userId, string role, CancellationToken ct);
    Task RemoveMemberAsync(Guid organizationId, Guid userId, CancellationToken ct);
    Task<TeamDto> CreateTeamAsync(Guid organizationId, string name, string? description, CancellationToken ct);
    Task<IReadOnlyList<TeamDto>> GetTeamsAsync(Guid organizationId, CancellationToken ct);
    Task<TeamDto> GetTeamAsync(Guid teamId, CancellationToken ct);
    Task UpdateTeamAsync(Guid teamId, string name, string? description, CancellationToken ct);
    Task DeleteTeamAsync(Guid teamId, CancellationToken ct);
    Task<IReadOnlyList<TeamMemberDto>> GetTeamMembersAsync(Guid teamId, CancellationToken ct);
    Task AddTeamMemberAsync(Guid teamId, Guid userId, string role, CancellationToken ct);
    Task RemoveTeamMemberAsync(Guid teamId, Guid userId, CancellationToken ct);
}

/// <summary>
/// Permissions: members read; owners/admins manage teams and settings; only the owner manages members
/// and deletes the organization.
/// </summary>
public sealed class OrganizationManagementService(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    IOrganizationAuthorizationService authorizationService,
    IIdentityService identityService) : IOrganizationManagementService
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 1000;

    private Guid CurrentUserId => currentUserService.UserId ?? throw new UnauthorizedAccessException();

    public async Task<Guid> CreateOrganizationAsync(string name, string? description, CancellationToken ct)
    {
        var ownerId = CurrentUserId;
        var organization = new Organization
        {
            Name = Guard.Required(name, "Organization name", NameMaxLength),
            Description = Guard.Optional(description, "Description", DescriptionMaxLength),
            OwnerId = ownerId,
            CreatedAtUtc = DateTime.UtcNow,
        };

        context.Organizations.Add(organization);
        context.OrganizationMembers.Add(new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = ownerId,
            Role = OrganizationRoles.Owner,
        });

        await context.SaveChangesAsync(ct);
        return organization.Id;
    }

    public async Task<IReadOnlyList<OrganizationDto>> GetMyOrganizationsAsync(CancellationToken ct)
    {
        var userId = CurrentUserId;
        return await context.OrganizationMembers
            .Where(member => member.UserId == userId)
            .OrderBy(member => member.Organization.Name)
            .Select(member => new OrganizationDto(
                member.Organization.Id,
                member.Organization.Name,
                member.Organization.Description,
                member.Organization.OwnerId,
                member.Role))
            .ToListAsync(ct);
    }

    public async Task<OrganizationDto> GetOrganizationAsync(Guid organizationId, CancellationToken ct)
    {
        await RequireMemberAsync(organizationId, ct);
        var organization = await FindOrganizationAsync(organizationId, ct);
        var myRole = await context.OrganizationMembers
            .Where(member => member.OrganizationId == organizationId && member.UserId == CurrentUserId)
            .Select(member => member.Role)
            .SingleOrDefaultAsync(ct);

        return new OrganizationDto(organization.Id, organization.Name, organization.Description, organization.OwnerId, myRole);
    }

    public async Task UpdateOrganizationAsync(Guid organizationId, string name, string? description, CancellationToken ct)
    {
        await RequireManagerAsync(organizationId, ct);
        var organization = await FindOrganizationAsync(organizationId, ct);

        organization.Name = Guard.Required(name, "Organization name", NameMaxLength);
        organization.Description = Guard.Optional(description, "Description", DescriptionMaxLength);
        organization.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteOrganizationAsync(Guid organizationId, CancellationToken ct)
    {
        await RequireOwnerAsync(organizationId, ct);
        context.Organizations.Remove(await FindOrganizationAsync(organizationId, ct));
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<OrganizationMemberDto>> GetMembersAsync(Guid organizationId, CancellationToken ct)
    {
        await RequireMemberAsync(organizationId, ct);

        var members = await context.OrganizationMembers
            .Where(member => member.OrganizationId == organizationId)
            .Select(member => new { member.UserId, member.Role })
            .ToListAsync(ct);

        var users = await identityService.GetUsersAsync(members.Select(member => member.UserId), ct);
        return members
            .OrderBy(member => RoleOrder(member.Role))
            .Select(member =>
            {
                var user = users.GetValueOrDefault(member.UserId);
                return new OrganizationMemberDto(member.UserId, member.Role, user?.FullName ?? "Unknown user", user?.Email ?? string.Empty);
            })
            .ToList();
    }

    public async Task AddMemberAsync(Guid organizationId, Guid userId, string role, CancellationToken ct)
    {
        await RequireOwnerAsync(organizationId, ct);
        if (!OrganizationRoles.IsValid(role) || role == OrganizationRoles.Owner)
        {
            throw new BadRequestException("Organization member role must be Admin or Member.");
        }

        if (await identityService.GetUserByIdAsync(userId, ct) is null)
        {
            throw new NotFoundException("User was not found.");
        }

        var existing = await context.OrganizationMembers
            .SingleOrDefaultAsync(member => member.OrganizationId == organizationId && member.UserId == userId, ct);
        if (existing?.Role == OrganizationRoles.Owner)
        {
            throw new BadRequestException("The organization owner's role cannot be changed.");
        }

        if (existing is null)
        {
            context.OrganizationMembers.Add(new OrganizationMember { OrganizationId = organizationId, UserId = userId, Role = role });
        }
        else
        {
            existing.Role = role;
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task RemoveMemberAsync(Guid organizationId, Guid userId, CancellationToken ct)
    {
        await RequireOwnerAsync(organizationId, ct);

        var member = await context.OrganizationMembers
            .SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.UserId == userId, ct)
            ?? throw new NotFoundException("Organization member was not found.");
        if (member.Role == OrganizationRoles.Owner)
        {
            throw new BadRequestException("The organization owner cannot be removed.");
        }

        context.OrganizationMembers.Remove(member);
        await context.SaveChangesAsync(ct);
    }

    public async Task<TeamDto> CreateTeamAsync(Guid organizationId, string name, string? description, CancellationToken ct)
    {
        await RequireManagerAsync(organizationId, ct);

        var team = new Team
        {
            OrganizationId = organizationId,
            Name = Guard.Required(name, "Team name", NameMaxLength),
            Description = Guard.Optional(description, "Description", DescriptionMaxLength),
            CreatedAtUtc = DateTime.UtcNow,
        };
        context.Teams.Add(team);
        await context.SaveChangesAsync(ct);
        return ToDto(team);
    }

    public async Task<IReadOnlyList<TeamDto>> GetTeamsAsync(Guid organizationId, CancellationToken ct)
    {
        await RequireMemberAsync(organizationId, ct);

        return await context.Teams
            .Where(team => team.OrganizationId == organizationId)
            .OrderBy(team => team.Name)
            .Select(team => new TeamDto(team.Id, team.OrganizationId, team.Name, team.Description))
            .ToListAsync(ct);
    }

    public async Task<TeamDto> GetTeamAsync(Guid teamId, CancellationToken ct)
    {
        var team = await FindTeamAsync(teamId, ct);
        await RequireMemberAsync(team.OrganizationId, ct);
        return ToDto(team);
    }

    public async Task UpdateTeamAsync(Guid teamId, string name, string? description, CancellationToken ct)
    {
        var team = await FindTeamAsync(teamId, ct);
        await RequireManagerAsync(team.OrganizationId, ct);

        team.Name = Guard.Required(name, "Team name", NameMaxLength);
        team.Description = Guard.Optional(description, "Description", DescriptionMaxLength);
        team.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteTeamAsync(Guid teamId, CancellationToken ct)
    {
        var team = await FindTeamAsync(teamId, ct);
        await RequireManagerAsync(team.OrganizationId, ct);

        context.Teams.Remove(team);
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<TeamMemberDto>> GetTeamMembersAsync(Guid teamId, CancellationToken ct)
    {
        var team = await FindTeamAsync(teamId, ct);
        await RequireMemberAsync(team.OrganizationId, ct);

        var members = await context.TeamMembers
            .Where(member => member.TeamId == teamId)
            .OrderBy(member => member.Role)
            .Select(member => new { member.UserId, member.Role })
            .ToListAsync(ct);

        var users = await identityService.GetUsersAsync(members.Select(member => member.UserId), ct);
        return members
            .Select(member =>
            {
                var user = users.GetValueOrDefault(member.UserId);
                return new TeamMemberDto(member.UserId, member.Role, user?.FullName ?? "Unknown user", user?.Email ?? string.Empty);
            })
            .ToList();
    }

    public async Task AddTeamMemberAsync(Guid teamId, Guid userId, string role, CancellationToken ct)
    {
        var team = await FindTeamAsync(teamId, ct);
        await RequireManagerAsync(team.OrganizationId, ct);

        if (!TeamRoles.IsValid(role))
        {
            throw new BadRequestException("Team member role must be Lead, Developer, or Viewer.");
        }

        if (!await context.OrganizationMembers.AnyAsync(member => member.OrganizationId == team.OrganizationId && member.UserId == userId, ct))
        {
            throw new BadRequestException("A team member must belong to the organization.");
        }

        var existing = await context.TeamMembers.SingleOrDefaultAsync(member => member.TeamId == teamId && member.UserId == userId, ct);
        if (existing is null)
        {
            context.TeamMembers.Add(new TeamMember { TeamId = teamId, UserId = userId, Role = role });
        }
        else
        {
            existing.Role = role;
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task RemoveTeamMemberAsync(Guid teamId, Guid userId, CancellationToken ct)
    {
        var team = await FindTeamAsync(teamId, ct);
        await RequireManagerAsync(team.OrganizationId, ct);

        var member = await context.TeamMembers.SingleOrDefaultAsync(item => item.TeamId == teamId && item.UserId == userId, ct)
            ?? throw new NotFoundException("Team member was not found.");
        context.TeamMembers.Remove(member);
        await context.SaveChangesAsync(ct);
    }

    private Task RequireMemberAsync(Guid organizationId, CancellationToken ct) =>
        authorizationService.RequireMemberAsync(organizationId, CurrentUserId, ct);

    private Task RequireManagerAsync(Guid organizationId, CancellationToken ct) =>
        authorizationService.RequireManagerAsync(organizationId, CurrentUserId, ct);

    private async Task RequireOwnerAsync(Guid organizationId, CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (!await context.OrganizationMembers.AnyAsync(
                member => member.OrganizationId == organizationId && member.UserId == userId && member.Role == OrganizationRoles.Owner,
                ct))
        {
            throw new ForbiddenAccessException("Organization owner access is required.");
        }
    }

    private static int RoleOrder(string role) => role switch
    {
        OrganizationRoles.Owner => 0,
        OrganizationRoles.Admin => 1,
        _ => 2,
    };

    private async Task<Organization> FindOrganizationAsync(Guid organizationId, CancellationToken ct) =>
        await context.Organizations.SingleOrDefaultAsync(organization => organization.Id == organizationId, ct)
        ?? throw new NotFoundException("Organization was not found.");

    private async Task<Team> FindTeamAsync(Guid teamId, CancellationToken ct) =>
        await context.Teams.SingleOrDefaultAsync(team => team.Id == teamId, ct)
        ?? throw new NotFoundException("Team was not found.");

    private static TeamDto ToDto(Team team) => new(team.Id, team.OrganizationId, team.Name, team.Description);
}
