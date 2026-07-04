using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Application.Features.SystemIntelligence.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Route("api/on-prem-agent")]
public sealed class OnPremAgentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IOnPremAgentService _agentService;

    public OnPremAgentController(IMediator mediator, IOnPremAgentService agentService)
    {
        _mediator = mediator;
        _agentService = agentService;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterOnPremAgentCommand command, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(command, cancellationToken));

    [HttpPost("{agentId:guid}/scan-results")]
    public async Task<IActionResult> SubmitScanResults(
        Guid agentId,
        [FromBody] SubmitScanResultsRequest request,
        CancellationToken cancellationToken)
    {
        await _agentService.AcceptScanPayloadAsync(agentId, request.ConnectionId, request.ScanResult, cancellationToken);
        return Ok(new { success = true });
    }

    [Authorize]
    [HttpGet]
    public IActionResult ListAgents() => Ok(_agentService.GetRegisteredAgents());
}

public sealed class SubmitScanResultsRequest
{
    public Guid ConnectionId { get; set; }
    public DatabaseSchemaScanResult ScanResult { get; set; } = new();
}
