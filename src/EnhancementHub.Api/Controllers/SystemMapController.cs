using EnhancementHub.Application.Features.SystemIntelligence.Commands;
using EnhancementHub.Application.Features.SystemIntelligence.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/system-map")]
public sealed class SystemMapController : ControllerBase
{
    private readonly IMediator _mediator;

    public SystemMapController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{applicationId:guid}")]
    public async Task<IActionResult> Get(Guid applicationId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetSystemMapQuery(applicationId), cancellationToken));

    [HttpPost("{applicationId:guid}/build")]
    public async Task<IActionResult> Build(Guid applicationId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new BuildSystemGraphCommand(applicationId), cancellationToken));

    [HttpGet("{applicationId:guid}/erd")]
    public async Task<IActionResult> Erd(Guid applicationId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetErdDiagramQuery(applicationId), cancellationToken));
}
