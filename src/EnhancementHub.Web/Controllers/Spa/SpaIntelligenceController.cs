using EnhancementHub.Application.AuditLogs;
using EnhancementHub.Application.Features.Repositories.Commands;
using EnhancementHub.Application.Features.Repositories.Queries;
using EnhancementHub.Application.Features.SystemIntelligence.Commands;
using EnhancementHub.Application.Features.SystemIntelligence.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Web.Controllers.Spa;

[ApiController]
[Authorize]
[Route("web-api/spa")]
public sealed class SpaIntelligenceController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpaIntelligenceController(IMediator mediator) => _mediator = mediator;

    [HttpGet("drift/connections")]
    public async Task<IActionResult> ListDriftConnections(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListDatabaseConnectionsQuery(), cancellationToken));

    [HttpGet("drift/report")]
    public async Task<IActionResult> GetDriftReport(
        [FromQuery] Guid connectionId,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetDriftReportQuery(connectionId), cancellationToken));

    [HttpGet("drift/request-draft")]
    public async Task<IActionResult> GetDriftRequestDraft(
        [FromQuery] Guid findingId,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetDriftRequestDraftQuery(findingId), cancellationToken));

    [HttpPost("drift/detect")]
    public async Task<IActionResult> DetectDrift(
        [FromBody] SpaDetectDriftRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new DetectSchemaDriftCommand(request.ConnectionId), cancellationToken));

    [HttpGet("repositories")]
    public async Task<IActionResult> ListRepositories(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListRepositoriesQuery(), cancellationToken));

    [HttpPost("repositories/{repositoryId:guid}/reindex")]
    public async Task<IActionResult> ReindexRepository(Guid repositoryId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new TriggerRepositoryIndexingCommand(repositoryId), cancellationToken);
        return Ok(new { message = "Indexing started." });
    }

    [HttpGet("audit/logs")]
    public async Task<IActionResult> ListAuditLogs(
        [FromQuery] string? entityType,
        [FromQuery] string? action,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new ListAuditLogsQuery(entityType, action, null, from, to),
            cancellationToken));

    [HttpGet("audit/export")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] string format,
        [FromQuery] string? entityType,
        [FromQuery] string? action,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ExportAuditLogsQuery(format, entityType, action, null, from, to),
            cancellationToken);

        return File(result.Content, result.ContentType, result.FileName);
    }
}

public sealed record SpaDetectDriftRequest(Guid ConnectionId);
