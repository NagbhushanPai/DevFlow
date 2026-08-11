using DevFlow.Application.Common.Interfaces;
using DevFlow.Domain.Entities;
using MediatR;

namespace DevFlow.Application.Organizations.Commands.CreateOrganization;

public sealed class CreateOrganizationHandler : IRequestHandler<CreateOrganizationCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateOrganizationHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var ownerId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            OwnerId = ownerId
        };

        _context.Organizations.Add(org);
        _context.OrganizationMembers.Add(new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            UserId = ownerId,
            Role = "Owner"
        });

        await _context.SaveChangesAsync(cancellationToken);

        return org.Id;
    }
}
