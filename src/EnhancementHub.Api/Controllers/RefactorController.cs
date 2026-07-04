using EnhancementHub.Application.Features.SystemIntelligence.Commands;
using EnhancementHub.Application.Features.SystemIntelligence.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/refactor")]
public sealed class RefactorController : ControllerBase
{
    private readonly IMediator _mediator;

    public RefactorController(IMediator mediator) => _mediator = mediator;

    [HttpPost("blast-radius")]
    public async Task<IActionResult> BlastRadius(
        [FromQuery] Guid applicationId,
        [FromQuery] string target,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new AnalyzeRefactorBlastRadiusCommand(applicationId, target), cancellationToken));

    [HttpPost("plans")]
    public async Task<IActionResult> GeneratePlan(
        [FromQuery] Guid applicationId,
        [FromQuery] string target,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GenerateRefactorPlanCommand(applicationId, target), cancellationToken));

    [HttpGet("plans")]
    public async Task<IActionResult> ListPlans(
        [FromQuery] Guid? applicationId,
        [FromQuery] Guid? connectionId,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListRefactorPlansQuery(applicationId, connectionId), cancellationToken));

    [HttpGet("plans/{planId:guid}")]
    public async Task<IActionResult> GetPlan(Guid planId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetRefactorPlanQuery(planId), cancellationToken));
}
