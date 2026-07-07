using EnhancementHub.Application.Features.Applications.Dtos;
using EnhancementHub.Application.Features.Applications.Queries;
using EnhancementHub.Application.Features.SystemIntelligence.Commands;
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

    [HttpGet("applications/{id:guid}")]
    public async Task<IActionResult> GetApplication(Guid id, CancellationToken cancellationToken)
    {
        var applications = await _mediator.Send(new ListApplicationsQuery(), cancellationToken);
        var application = applications.FirstOrDefault(a => a.Id == id);
        if (application is null)
        {
            return NotFound();
        }

        var profiles = await _mediator.Send(new GetApplicationProfileQuery(id), cancellationToken);
        return Ok(new SpaApplicationDetailResponse(application, profiles));
    }

    [HttpGet("system-map/{applicationId:guid}")]
    public async Task<IActionResult> GetSystemMap(Guid applicationId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetSystemMapQuery(applicationId), cancellationToken));

    [HttpPost("system-map/{applicationId:guid}/rebuild")]
    public async Task<IActionResult> RebuildSystemMap(Guid applicationId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new BuildSystemGraphCommand(applicationId), cancellationToken));
}
