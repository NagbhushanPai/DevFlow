using DevFlow.Application.Common.Exceptions;
using DevFlow.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DevFlow.API.Controllers;

[Route("api/users")]
public sealed class UsersController(IIdentityService identity, ICurrentUserService currentUser) : BaseApiController
{
    [HttpGet("me")]
    public async Task<ActionResult<UserLookupResponse>> Me(CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var user = await identity.GetUserByIdAsync(userId, ct) ?? throw new NotFoundException("User was not found.");
        return Ok(new UserLookupResponse(user.Id, user.FirstName, user.LastName, user.Email));
    }

    /// <summary>
    /// Finds a user by exact email address so they can be added to an organization. Exact match only, to avoid
    /// exposing a searchable user directory.
    /// </summary>
    [HttpGet("lookup")]
    public async Task<ActionResult<UserLookupResponse>> Lookup([FromQuery] string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("An email address is required.");
        }

        var user = await identity.GetUserByEmailAsync(email.Trim(), ct) ?? throw new NotFoundException("No user with that email address.");
        return Ok(new UserLookupResponse(user.Id, user.FirstName, user.LastName, user.Email));
    }
}

public sealed record UserLookupResponse(Guid Id, string FirstName, string LastName, string Email);
