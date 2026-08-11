using DevFlow.Application.Organizations.DTOs;
using MediatR;

namespace DevFlow.Application.Organizations.Queries.GetOrganizations;

public sealed record GetOrganizationsQuery() : IRequest<IReadOnlyList<OrganizationDto>>;
