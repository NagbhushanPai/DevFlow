using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Organizations.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Mapster;

namespace DevFlow.Application.Organizations.Queries.GetOrganizations;

public sealed class GetOrganizationsHandler : IRequestHandler<GetOrganizationsQuery, IReadOnlyList<OrganizationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetOrganizationsHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<OrganizationDto>> Handle(GetOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var orgs = await _context.OrganizationMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.Organization.Adapt<OrganizationDto>())
            .ToListAsync(cancellationToken);

        return orgs;
    }
}
