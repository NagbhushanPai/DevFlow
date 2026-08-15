using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevFlow.Application.Common.Authorization;

public interface IOrganizationAuthorizationService
{
    Task RequireMemberAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken);
    Task RequireManagerAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken);
}

public sealed class OrganizationAuthorizationService(IApplicationDbContext context)
    : IOrganizationAuthorizationService
{
    public async Task RequireMemberAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var exists = await context.OrganizationMembers.AnyAsync(
            member => member.OrganizationId == organizationId && member.UserId == userId,
            cancellationToken);

        if (!exists)
        {
            throw new ForbiddenAccessException("You are not a member of this organization.");
        }
    }

    public async Task RequireManagerAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var role = await context.OrganizationMembers
            .Where(member => member.OrganizationId == organizationId && member.UserId == userId)
            .Select(member => member.Role)
            .SingleOrDefaultAsync(cancellationToken);

        if (role is null || !OrganizationRoles.CanManage(role))
        {
            throw new ForbiddenAccessException("Organization owner or admin access is required.");
        }
    }
}
