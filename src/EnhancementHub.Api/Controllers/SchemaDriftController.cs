using EnhancementHub.Application.Features.SystemIntelligence.Commands;
using EnhancementHub.Application.Features.SystemIntelligence.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/schema-drift")]
public sealed class SchemaDriftController : ControllerBase
{
    private readonly IMediator _mediator;

    public SchemaDriftController(IMediator mediator) => _mediator = mediator;

    [HttpPost("detect")]
    public async Task<IActionResult> Detect(
        [FromQuery] Guid connectionId,
        [FromQuery] Guid? repositoryId,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new DetectSchemaDriftCommand(connectionId, repositoryId), cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Report(
        [FromQuery] Guid connectionId,
        [FromQuery] Guid? repositoryId,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetDriftReportQuery(connectionId, repositoryId), cancellationToken));

    [HttpPost("findings/{findingId:guid}/resolve")]
    public async Task<IActionResult> ResolveFinding(Guid findingId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ResolveSchemaDriftFindingCommand(findingId), cancellationToken));
}
