using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevFlow.API.Controllers;

[ApiController]
[Route("api/test")]
public sealed class TestController : ControllerBase
{
    [HttpGet("public")]
    [AllowAnonymous]
    public IActionResult Public()
    {
        return Ok(new
        {
            Message = "Public endpoint works."
        });
    }

    [HttpGet("protected")]
    [Authorize]
    public IActionResult Protected()
    {
        return Ok(new
        {
            Message = "Authenticated endpoint works."
        });
    }

    [HttpGet("developer")]
    [Authorize(Roles = "Developer")]
    public IActionResult Developer()
    {
        return Ok(new
        {
            Message = "Developer authorization works."
        });
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult Admin()
    {
        return Ok(new
        {
            Message = "Admin authorization works."
        });
    }
}