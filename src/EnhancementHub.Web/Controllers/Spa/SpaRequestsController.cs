using System.Text;
using EnhancementHub.Application.Features.Analysis.Queries;
using EnhancementHub.Application.Features.Analysis.Queries;
using EnhancementHub.Application.Features.Approvals.Commands;
using EnhancementHub.Application.Features.Approvals.Queries;
using EnhancementHub.Application.Features.Applications.Queries;
using EnhancementHub.Application.Features.EnhancementRequests.Commands;
using EnhancementHub.Application.Features.EnhancementRequests.Queries;
using EnhancementHub.Application.Features.Templates.Queries;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Web.Controllers.Spa;

[ApiController]
[Authorize]
[Route("web-api/spa")]
public sealed class SpaRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpaRequestsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("requests")]
    public async Task<IActionResult> ListRequests(
        [FromQuery] string? q,
        [FromQuery] EnhancementRequestStatus? status,
        [FromQuery] string? priority,
        [FromQuery] string? view,
        [FromQuery] EnhancementRequestSort sort = EnhancementRequestSort.Newest,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        RiskLevel? minRisk = view == "highrisk" ? RiskLevel.High : null;
        var search = q;
        if (view == "mine" && User.Identity?.Name is not null)
        {
            search = User.Identity.Name.Contains('@')
                ? User.Identity.Name.Split('@')[0]
                : User.Identity.Name;
        }

        var result = await _mediator.Send(
            new ListEnhancementRequestsQuery(
                status,
                Search: search,
                Priority: priority,
                MinRisk: minRisk,
                Sort: sort,
                Page: page,
                PageSize: pageSize),
            cancellationToken);

        return Ok(new
        {
            items = result.Items,
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
            totalPages = result.TotalPages,
        });
    }

    [HttpPost("requests/export")]
    public async Task<IActionResult> ExportRequests(
        [FromBody] SpaExportRequestsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Ids is not { Count: > 0 })
        {
            return BadRequest(new { message = "Select at least one request to export." });
        }

        var result = await _mediator.Send(
            new ListEnhancementRequestsQuery(Ids: request.Ids),
            cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("Id,Title,Status,Priority,Risk,Application,SubmittedBy,CreatedAt");
        foreach (var item in result.Items)
        {
            csv.AppendLine(string.Join(',',
                Csv(item.Id.ToString()),
                Csv(item.Title),
                Csv(item.Status.ToString()),
                Csv(item.Priority),
                Csv(item.LatestRiskLevel?.ToString() ?? ""),
                Csv(item.TargetApplicationName ?? ""),
                Csv(item.SubmittedByUserName ?? ""),
                Csv(item.CreatedAt.ToString("O"))));
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"enhancement-requests-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    [HttpGet("requests/{id:guid}")]
    public async Task<IActionResult> GetRequest(Guid id, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetEnhancementRequestByIdQuery(id), cancellationToken));

    [HttpGet("analysis/{requestId:guid}")]
    public async Task<IActionResult> GetAnalysis(Guid requestId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _mediator.Send(new GetEnhancementAnalysisQuery(requestId), cancellationToken));
        }
        catch (Application.Common.Exceptions.NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("analysis/{requestId:guid}/evolution")]
    public async Task<IActionResult> GetAnalysisEvolution(Guid requestId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _mediator.Send(new GetRequestAnalysisEvolutionQuery(requestId), cancellationToken));
        }
        catch (Application.Common.Exceptions.NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("requests/{id:guid}/approval-history")]
    public async Task<IActionResult> GetApprovalHistory(Guid id, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetApprovalHistoryQuery(id), cancellationToken));

    [HttpPost("requests/{id:guid}/comments")]
    public async Task<IActionResult> AddComment(
        Guid id,
        [FromBody] SpaAddCommentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new AddCommentCommand(id, request.Content, request.IsInternal),
            cancellationToken));

    [HttpGet("requests/create-form")]
    public async Task<IActionResult> GetCreateForm(CancellationToken cancellationToken)
    {
        var applications = await _mediator.Send(new ListApplicationsQuery(), cancellationToken);
        var templates = await _mediator.Send(new ListEnhancementTemplatesQuery(), cancellationToken);
        return Ok(new SpaCreateRequestFormResponse(applications, templates));
    }

    [HttpGet("templates/{id:guid}")]
    public async Task<IActionResult> GetTemplate(Guid id, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetEnhancementTemplateQuery(id), cancellationToken));

    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest(
        [FromBody] SpaCreateRequestInput request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new CreateEnhancementRequestCommand(
                request.Title,
                request.BusinessDescription,
                request.DesiredOutcome,
                request.Priority,
                request.TargetApplicationId,
                request.RequestedDueDate,
                request.Department,
                null,
                request.SupportingNotes,
                request.TemplateId),
            cancellationToken));

    private static string Csv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}

public sealed record SpaExportRequestsRequest(IReadOnlyList<Guid> Ids);
