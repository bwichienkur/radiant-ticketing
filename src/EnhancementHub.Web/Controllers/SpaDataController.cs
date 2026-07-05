using EnhancementHub.Application.Features.Analysis.Queries;
using EnhancementHub.Application.Features.Applications.Queries;
using EnhancementHub.Application.Features.EnhancementRequests.Queries;
using EnhancementHub.Application.Features.SystemIntelligence.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Web.Controllers;

[ApiController]
[Authorize]
[Route("web-api/spa")]
public sealed class SpaDataController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpaDataController(IMediator mediator) => _mediator = mediator;

    [HttpGet("requests/{id:guid}")]
    public async Task<IActionResult> GetRequest(Guid id, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetEnhancementRequestByIdQuery(id), cancellationToken));

    [HttpGet("analysis/{requestId:guid}")]
    public async Task<IActionResult> GetAnalysis(Guid requestId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _mediator.Send(new GetEnhancementAnalysisQuery(requestId), cancellationToken));
        }
        catch (Application.Common.Exceptions.NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("applications")]
    public async Task<IActionResult> ListApplications(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListApplicationsQuery(), cancellationToken));

    [HttpGet("system-map/{applicationId:guid}")]
    public async Task<IActionResult> GetSystemMap(Guid applicationId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetSystemMapQuery(applicationId), cancellationToken));
}
