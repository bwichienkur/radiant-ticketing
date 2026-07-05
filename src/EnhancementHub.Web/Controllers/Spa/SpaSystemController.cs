using EnhancementHub.Application.Features.Applications.Queries;
using EnhancementHub.Application.Features.SystemIntelligence.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Web.Controllers.Spa;

[ApiController]
[Authorize]
[Route("web-api/spa")]
public sealed class SpaSystemController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpaSystemController(IMediator mediator) => _mediator = mediator;

    [HttpGet("applications")]
    public async Task<IActionResult> ListApplications(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListApplicationsQuery(), cancellationToken));

    [HttpGet("system-map/{applicationId:guid}")]
    public async Task<IActionResult> GetSystemMap(Guid applicationId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetSystemMapQuery(applicationId), cancellationToken));
}
