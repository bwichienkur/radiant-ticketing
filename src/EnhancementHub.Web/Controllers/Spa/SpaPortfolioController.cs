using EnhancementHub.Application.Features.Reporting.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Web.Controllers.Spa;

[ApiController]
[Authorize(Roles = "Admin,Approver")]
[Route("web-api/spa/portfolio")]
public sealed class SpaPortfolioController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpaPortfolioController(IMediator mediator) => _mediator = mediator;

    [HttpGet("health")]
    public async Task<IActionResult> GetPortfolioHealth(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetPortfolioHealthQuery(), cancellationToken));
}
