using System.Text;
using EnhancementHub.Application.AuditLogs;
using EnhancementHub.Application.Features.Repositories.Commands;
using EnhancementHub.Application.Features.Repositories.Queries;
using EnhancementHub.Application.Features.SystemIntelligence.Commands;
using EnhancementHub.Application.Features.SystemIntelligence.Queries;
using EnhancementHub.Domain.Enums;
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

    [HttpGet("connections")]
    public async Task<IActionResult> ListConnections(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListDatabaseConnectionsQuery(), cancellationToken));

    [HttpPost("connections")]
    public async Task<IActionResult> RegisterConnection(
        [FromBody] SpaRegisterConnectionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new RegisterDatabaseConnectionCommand(
                request.ApplicationId,
                request.Name,
                request.Provider,
                request.ConnectionString,
                request.IsReadOnly),
            cancellationToken));

    [HttpPost("connections/{connectionId:guid}/scan")]
    public async Task<IActionResult> ScanConnection(Guid connectionId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new TriggerDatabaseScanCommand(connectionId), cancellationToken));

    [HttpGet("connections/{connectionId:guid}/schema")]
    public async Task<IActionResult> GetConnectionSchema(Guid connectionId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetDatabaseSchemaQuery(connectionId), cancellationToken));

    [HttpGet("connections/{connectionId:guid}/erd")]
    public async Task<IActionResult> GetConnectionErd(Guid connectionId, CancellationToken cancellationToken)
    {
        var connections = await _mediator.Send(new ListDatabaseConnectionsQuery(), cancellationToken);
        var connection = connections.FirstOrDefault(c => c.Id == connectionId);
        if (connection is null)
        {
            return NotFound();
        }

        return Ok(await _mediator.Send(new GetErdDiagramQuery(connection.ApplicationId), cancellationToken));
    }

    [HttpGet("documentation/export")]
    public async Task<IActionResult> ExportDocumentation(
        [FromQuery] Guid applicationId,
        [FromQuery] DocumentationExportFormat format,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ExportDocumentationCommand(applicationId, format), cancellationToken);
        return File(Encoding.UTF8.GetBytes(result.Content), result.ContentType, result.FileName);
    }

    [HttpPost("refactor/analyze")]
    public async Task<IActionResult> AnalyzeRefactor(
        [FromBody] SpaRefactorAnalyzeRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new AnalyzeRefactorBlastRadiusCommand(request.ApplicationId, request.Target),
            cancellationToken));

    [HttpPost("refactor/plans")]
    public async Task<IActionResult> GenerateRefactorPlan(
        [FromBody] SpaRefactorAnalyzeRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new GenerateRefactorPlanCommand(request.ApplicationId, request.Target),
            cancellationToken));

    [HttpGet("refactor/plans")]
    public async Task<IActionResult> ListRefactorPlans(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListRefactorPlansQuery(), cancellationToken));

    [HttpGet("refactor/plans/{planId:guid}")]
    public async Task<IActionResult> GetRefactorPlan(Guid planId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetRefactorPlanQuery(planId), cancellationToken));

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

public sealed record SpaRegisterConnectionRequest(
    Guid ApplicationId,
    string Name,
    DatabaseProviderType Provider,
    string ConnectionString,
    bool IsReadOnly);

public sealed record SpaRefactorAnalyzeRequest(Guid ApplicationId, string Target);
