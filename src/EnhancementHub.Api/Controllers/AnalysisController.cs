using EnhancementHub.Application.Features.Analysis.Commands;
using EnhancementHub.Application.Features.Analysis.Queries;
using EnhancementHub.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
    [EnableRateLimiting(RateLimitingExtensions.AiAnalysisPolicy)]
    public async Task<IActionResult> Trigger(Guid requestId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new TriggerAiAnalysisCommand(requestId), cancellationToken));

    [HttpPost("{requestId:guid}/reanalyze")]
    [EnableRateLimiting(RateLimitingExtensions.AiAnalysisPolicy)]
    public async Task<IActionResult> Reanalyze(Guid requestId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new RequestReanalysisCommand(requestId), cancellationToken));

    [HttpGet("{requestId:guid}/compare")]
    public async Task<IActionResult> Compare(
        Guid requestId,
        [FromQuery] int versionA,
        [FromQuery] int? versionB,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new GetAnalysisComparisonQuery(requestId, versionA, versionB),
            cancellationToken));

    [HttpPost("findings/{findingId:guid}/approve")]
    public async Task<IActionResult> ApproveFinding(
        Guid findingId,
        [FromBody] ApproveFindingRequest? body,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new ApproveAnalysisFindingCommand(findingId, body?.Approved ?? true),
            cancellationToken));

    [HttpPost("{requestId:guid}/architect-edit")]
    public async Task<IActionResult> RecordArchitectEdit(
        Guid requestId,
        [FromBody] ArchitectEditRequest body,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new RecordArchitectAnalysisEditCommand(
                requestId,
                body.EnhancementAnalysisId,
                body.FeatureSummary,
                body.TechnicalRequirements,
                body.TestingPlan,
                body.RolloutPlan,
                body.Comments),
            cancellationToken));

    public sealed record ApproveFindingRequest(bool Approved = true);

    public sealed record ArchitectEditRequest(
        Guid EnhancementAnalysisId,
        string? FeatureSummary,
        string? TechnicalRequirements,
        string? TestingPlan,
        string? RolloutPlan,
        string? Comments);
}
