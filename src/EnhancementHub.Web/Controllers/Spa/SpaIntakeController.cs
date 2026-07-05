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

    public SpaIntakeController(IMediator mediator) => _mediator = mediator;

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

    [HttpPost("sessions/{id:guid}/create-request")]
    public async Task<IActionResult> CreateRequest(Guid id, CancellationToken cancellationToken)
    {
        var requestId = await _mediator.Send(new CreateRequestFromIntakeSessionCommand(id), cancellationToken);
        return Ok(new SpaCreatedRequestResponse(requestId));
    }
}

public sealed record SpaStartIntakeSessionRequest(string? InitialPrompt);

public sealed record SpaIntakeMessageRequest(string Message);

public sealed record SpaCreatedRequestResponse(Guid Id);
