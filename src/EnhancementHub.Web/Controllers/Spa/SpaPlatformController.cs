using EnhancementHub.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Web.Controllers.Spa;

[ApiController]
[Authorize]
[Route("web-api/spa")]
public sealed class SpaPlatformController : ControllerBase
{
    private readonly IPlatformRuntimeStatusService _runtimeStatus;

    public SpaPlatformController(IPlatformRuntimeStatusService runtimeStatus) =>
        _runtimeStatus = runtimeStatus;

    [HttpGet("platform/runtime-status")]
    public IActionResult GetRuntimeStatus() => Ok(_runtimeStatus.GetStatus());
}
