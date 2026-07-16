using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DevFlow.API.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    private ISender? _sender;

    protected ISender Sender =>
        _sender ??=
        HttpContext.RequestServices.GetRequiredService<ISender>();
}