using EnhancementHub.Application.Features.Analysis.Commands;
using EnhancementHub.Application.Features.Analysis.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AnalysisController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnalysisController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{requestId:guid}")]
    public async Task<IActionResult> Get(Guid requestId, [FromQuery] int? version, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetEnhancementAnalysisQuery(requestId, version), cancellationToken));

    [HttpPost("{requestId:guid}/trigger")]
    public async Task<IActionResult> Trigger(Guid requestId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new TriggerAiAnalysisCommand(requestId), cancellationToken));

    [HttpPost("{requestId:guid}/reanalyze")]
    public async Task<IActionResult> Reanalyze(Guid requestId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new RequestReanalysisCommand(requestId), cancellationToken));
}
