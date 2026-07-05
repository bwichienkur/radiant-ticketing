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

        return Ok(await _mediator.Send(
            new ListEnhancementRequestsQuery(
                status,
                Search: search,
                Priority: priority,
                MinRisk: minRisk,
                Sort: sort),
            cancellationToken));
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
}
