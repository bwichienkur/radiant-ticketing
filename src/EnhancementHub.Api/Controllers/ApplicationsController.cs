using EnhancementHub.Application.Features.Applications.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ApplicationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ApplicationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListApplicationsQuery(), cancellationToken));

    [HttpGet("{id:guid}/profile")]
    public async Task<IActionResult> Profile(Guid id, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetApplicationProfileQuery(id), cancellationToken));
}
