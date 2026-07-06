using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.IntakeCopilot.Commands;
using EnhancementHub.Application.Features.IntakeCopilot.Dtos;
using EnhancementHub.Application.Features.IntakeCopilot.Queries;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Web.Controllers.Spa;

[ApiController]
[Authorize]
[Route("web-api/spa/intake")]
public sealed class SpaIntakeController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAiUsageBudgetService _budgetService;

    public SpaIntakeController(IMediator mediator, IAiUsageBudgetService budgetService)
    {
        _mediator = mediator;
        _budgetService = budgetService;
    }

    [HttpGet("budget")]
    public async Task<IActionResult> GetBudgetStatus(CancellationToken cancellationToken) =>
        Ok(await _budgetService.GetStatusAsync(cancellationToken));

    [HttpPost("score-draft")]
    public async Task<IActionResult> ScoreDraft(
        [FromBody] ScoreIntakeDraftRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ScoreIntakeDraftQuery(request), cancellationToken));

    [HttpPost("sessions")]
    public async Task<IActionResult> StartSession(
        [FromBody] SpaStartIntakeSessionRequest? request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new StartIntakeCopilotSessionCommand(
                request?.InitialPrompt,
                IntakeCopilotSource.Web),
            cancellationToken));

    [HttpGet("sessions/{id:guid}")]
    public async Task<IActionResult> GetSession(Guid id, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetIntakeCopilotSessionQuery(id), cancellationToken));

    [HttpPost("sessions/{id:guid}/messages")]
    public async Task<IActionResult> SendMessage(
        Guid id,
        [FromBody] SpaIntakeMessageRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new SendIntakeCopilotMessageCommand(id, request.Message), cancellationToken));

    [HttpPost("sessions/{id:guid}/policy-document")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> AttachPolicyDocument(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { message = "Policy document file is required." });
        }

        await using var stream = file.OpenReadStream();
        return Ok(await _mediator.Send(
            new AttachPolicyDocumentCommand(id, file.FileName, stream),
            cancellationToken));
    }

    [HttpPost("sessions/{id:guid}/policy-url")]
    public async Task<IActionResult> AttachPolicyUrl(
        Guid id,
        [FromBody] SpaIntakePolicyUrlRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new AttachPolicyUrlCommand(id, request.Url), cancellationToken));

    [HttpPost("sessions/{id:guid}/create-request")]
    public async Task<IActionResult> CreateRequest(
        Guid id,
        [FromBody] SpaIntakeCreateRequestRequest? request,
        CancellationToken cancellationToken)
    {
        var requestId = await _mediator.Send(
            new CreateRequestFromIntakeSessionCommand(id, request?.Overrides),
            cancellationToken);
        return Ok(new SpaCreatedRequestResponse(requestId));
    }
}

public sealed record SpaStartIntakeSessionRequest(string? InitialPrompt);

public sealed record SpaIntakeMessageRequest(string Message);

public sealed record SpaIntakePolicyUrlRequest(string Url);

public sealed record SpaIntakeCreateRequestRequest(IntakeCopilotSubmitOverridesDto? Overrides);

public sealed record SpaCreatedRequestResponse(Guid Id);
