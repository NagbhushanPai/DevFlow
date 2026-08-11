using DevFlow.Application.Organizations.Commands.CreateOrganization;
using DevFlow.Application.Organizations.Queries.GetOrganizations;
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

    public OrganizationsController(ISender sender)
    {
        _sender = sender;
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
}
