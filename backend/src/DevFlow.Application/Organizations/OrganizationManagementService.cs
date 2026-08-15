using DevFlow.Application.Common.Authorization;
using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Organizations.DTOs;
using DevFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevFlow.Application.Organizations;

public interface IOrganizationManagementService
{
    Task<OrganizationDto> GetOrganizationAsync(Guid organizationId, CancellationToken cancellationToken);
    Task UpdateOrganizationAsync(Guid organizationId, string name, string? description, CancellationToken cancellationToken);
    Task DeleteOrganizationAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrganizationMemberDto>> GetMembersAsync(Guid organizationId, CancellationToken cancellationToken);
    Task AddMemberAsync(Guid organizationId, Guid userId, string role, CancellationToken cancellationToken);
    Task RemoveMemberAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken);
    Task<TeamDto> CreateTeamAsync(Guid organizationId, string name, string? description, CancellationToken cancellationToken);
    Task<IReadOnlyList<TeamDto>> GetTeamsAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<TeamDto> GetTeamAsync(Guid teamId, CancellationToken cancellationToken);
    Task UpdateTeamAsync(Guid teamId, string name, string? description, CancellationToken cancellationToken);
    Task DeleteTeamAsync(Guid teamId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TeamMemberDto>> GetTeamMembersAsync(Guid teamId, CancellationToken cancellationToken);
    Task AddTeamMemberAsync(Guid teamId, Guid userId, string role, CancellationToken cancellationToken);
    Task RemoveTeamMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken);
}

public sealed class OrganizationManagementService(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    IOrganizationAuthorizationService authorizationService)
    : IOrganizationManagementService
{
    public async Task<OrganizationDto> GetOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        await RequireMemberAsync(organizationId, cancellationToken);
        return ToDto(await FindOrganizationAsync(organizationId, cancellationToken));
    }

    public async Task UpdateOrganizationAsync(Guid organizationId, string name, string? description, CancellationToken cancellationToken)
    {
        await RequireManagerAsync(organizationId, cancellationToken);
        var organization = await FindOrganizationAsync(organizationId, cancellationToken);
        organization.Name = RequiredText(name, "Organization name");
        organization.Description = OptionalText(description);
        organization.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        await RequireOwnerAsync(organizationId, cancellationToken);
        context.Organizations.Remove(await FindOrganizationAsync(organizationId, cancellationToken));
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationMemberDto>> GetMembersAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        await RequireMemberAsync(organizationId, cancellationToken);
        return await context.OrganizationMembers.Where(member => member.OrganizationId == organizationId)
            .OrderBy(member => member.Role).Select(member => new OrganizationMemberDto(member.UserId, member.Role)).ToListAsync(cancellationToken);
    }

    public async Task AddMemberAsync(Guid organizationId, Guid userId, string role, CancellationToken cancellationToken)
    {
        await RequireOwnerAsync(organizationId, cancellationToken);
        if (!OrganizationRoles.IsValid(role) || role == OrganizationRoles.Owner)
            throw new ArgumentException("Organization member role must be Admin or Member.");
        var existing = await context.OrganizationMembers.SingleOrDefaultAsync(member => member.OrganizationId == organizationId && member.UserId == userId, cancellationToken);
        if (existing is null)
            context.OrganizationMembers.Add(new OrganizationMember { Id = Guid.NewGuid(), OrganizationId = organizationId, UserId = userId, Role = role });
        else
            existing.Role = role;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveMemberAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        await RequireOwnerAsync(organizationId, cancellationToken);
        var member = await context.OrganizationMembers.SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Organization member was not found.");
        if (member.Role == OrganizationRoles.Owner)
            throw new ArgumentException("The organization owner cannot be removed.");
        context.OrganizationMembers.Remove(member);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TeamDto> CreateTeamAsync(Guid organizationId, string name, string? description, CancellationToken cancellationToken)
    {
        await RequireManagerAsync(organizationId, cancellationToken);
        var team = new Team { Id = Guid.NewGuid(), OrganizationId = organizationId, Name = RequiredText(name, "Team name"), Description = OptionalText(description) };
        context.Teams.Add(team);
        await context.SaveChangesAsync(cancellationToken);
        return ToDto(team);
    }

    public async Task<IReadOnlyList<TeamDto>> GetTeamsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        await RequireMemberAsync(organizationId, cancellationToken);
        return await context.Teams.Where(team => team.OrganizationId == organizationId).OrderBy(team => team.Name)
            .Select(team => new TeamDto(team.Id, team.OrganizationId, team.Name, team.Description)).ToListAsync(cancellationToken);
    }

    public async Task<TeamDto> GetTeamAsync(Guid teamId, CancellationToken cancellationToken)
    {
        var team = await FindTeamAsync(teamId, cancellationToken);
        await RequireMemberAsync(team.OrganizationId, cancellationToken);
        return ToDto(team);
    }

    public async Task UpdateTeamAsync(Guid teamId, string name, string? description, CancellationToken cancellationToken)
    {
        var team = await FindTeamAsync(teamId, cancellationToken);
        await RequireManagerAsync(team.OrganizationId, cancellationToken);
        team.Name = RequiredText(name, "Team name"); team.Description = OptionalText(description); team.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTeamAsync(Guid teamId, CancellationToken cancellationToken)
    {
        var team = await FindTeamAsync(teamId, cancellationToken);
        await RequireManagerAsync(team.OrganizationId, cancellationToken);
        context.Teams.Remove(team);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TeamMemberDto>> GetTeamMembersAsync(Guid teamId, CancellationToken cancellationToken)
    {
        var team = await FindTeamAsync(teamId, cancellationToken);
        await RequireMemberAsync(team.OrganizationId, cancellationToken);
        return await context.TeamMembers.Where(member => member.TeamId == teamId).OrderBy(member => member.Role)
            .Select(member => new TeamMemberDto(member.UserId, member.Role)).ToListAsync(cancellationToken);
    }

    public async Task AddTeamMemberAsync(Guid teamId, Guid userId, string role, CancellationToken cancellationToken)
    {
        var team = await FindTeamAsync(teamId, cancellationToken);
        await RequireManagerAsync(team.OrganizationId, cancellationToken);
        if (!TeamRoles.IsValid(role)) throw new ArgumentException("Team member role must be Lead, Developer, or Viewer.");
        if (!await context.OrganizationMembers.AnyAsync(member => member.OrganizationId == team.OrganizationId && member.UserId == userId, cancellationToken))
            throw new ArgumentException("A team member must belong to the organization.");
        var existing = await context.TeamMembers.SingleOrDefaultAsync(member => member.TeamId == teamId && member.UserId == userId, cancellationToken);
        if (existing is null)
            context.TeamMembers.Add(new TeamMember { Id = Guid.NewGuid(), TeamId = teamId, UserId = userId, Role = role });
        else
            existing.Role = role;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveTeamMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken)
    {
        var team = await FindTeamAsync(teamId, cancellationToken);
        await RequireManagerAsync(team.OrganizationId, cancellationToken);
        var member = await context.TeamMembers.SingleOrDefaultAsync(item => item.TeamId == teamId && item.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Team member was not found.");
        context.TeamMembers.Remove(member);
        await context.SaveChangesAsync(cancellationToken);
    }

    private Task RequireMemberAsync(Guid organizationId, CancellationToken cancellationToken) => authorizationService.RequireMemberAsync(organizationId, CurrentUserId, cancellationToken);
    private Task RequireManagerAsync(Guid organizationId, CancellationToken cancellationToken) => authorizationService.RequireManagerAsync(organizationId, CurrentUserId, cancellationToken);
    private async Task RequireOwnerAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        if (!await context.OrganizationMembers.AnyAsync(member => member.OrganizationId == organizationId && member.UserId == CurrentUserId && member.Role == OrganizationRoles.Owner, cancellationToken))
            throw new ForbiddenAccessException("Organization owner access is required.");
    }
    private Guid CurrentUserId => currentUserService.UserId ?? throw new UnauthorizedAccessException();
    private async Task<Organization> FindOrganizationAsync(Guid organizationId, CancellationToken cancellationToken) => await context.Organizations.SingleOrDefaultAsync(organization => organization.Id == organizationId, cancellationToken) ?? throw new NotFoundException("Organization was not found.");
    private async Task<Team> FindTeamAsync(Guid teamId, CancellationToken cancellationToken) => await context.Teams.SingleOrDefaultAsync(team => team.Id == teamId, cancellationToken) ?? throw new NotFoundException("Team was not found.");
    private static OrganizationDto ToDto(Organization organization) => new() { Id = organization.Id, Name = organization.Name, Description = organization.Description, OwnerId = organization.OwnerId };
    private static TeamDto ToDto(Team team) => new(team.Id, team.OrganizationId, team.Name, team.Description);
    private static string RequiredText(string value, string fieldName) { var trimmed = value?.Trim(); return string.IsNullOrWhiteSpace(trimmed) ? throw new ArgumentException($"{fieldName} is required.") : trimmed; }
    private static string? OptionalText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
