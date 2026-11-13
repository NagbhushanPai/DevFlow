using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevFlow.API.Controllers;

/// <summary>Base for every API controller: API conventions and authenticated by default.</summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase;
