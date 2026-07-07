using EnhancementHub.Application.Features.Search.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Web.Controllers.Spa;

[ApiController]
[Authorize]
[Route("web-api/spa")]
public sealed class SpaSearchController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpaSearchController(IMediator mediator) => _mediator = mediator;

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] bool grouped = false,
        [FromQuery] bool semantic = false,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GlobalEntitySearchQuery(q, limit, semantic), cancellationToken);
        return grouped ? Ok(result) : Ok(result.Items);
    }
}
