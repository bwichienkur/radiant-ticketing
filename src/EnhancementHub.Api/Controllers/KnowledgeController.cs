using EnhancementHub.Application.Features.Knowledge.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class KnowledgeController : ControllerBase
{
    private readonly IMediator _mediator;

    public KnowledgeController(IMediator mediator) => _mediator = mediator;

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int topK = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { error = "Query parameter 'q' is required." });
        }

        return Ok(await _mediator.Send(new SearchKnowledgeQuery(q, TopK: topK), cancellationToken));
    }
}
