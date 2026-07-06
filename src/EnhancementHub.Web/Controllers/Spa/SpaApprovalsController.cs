using EnhancementHub.Application.Features.Approvals.Commands;
using EnhancementHub.Application.Features.Approvals.Queries;
using EnhancementHub.Application.Features.EnhancementRequests.Queries;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Web.Controllers.Spa;

[ApiController]
[Authorize]
[Route("web-api/spa/approvals")]
public sealed class SpaApprovalsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpaApprovalsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("pending")]
    public async Task<IActionResult> ListPendingApprovals(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ListEnhancementRequestsQuery(
                EnhancementRequestStatus.PendingApproval,
                Sort: EnhancementRequestSort.HighestRisk),
            cancellationToken);

        return Ok(result.Items);
    }

    [HttpGet("{id:guid}/recommendation")]
    public async Task<IActionResult> GetApprovalRecommendation(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetApprovalRecommendationQuery(id), cancellationToken));

    [HttpPost("{id:guid}/action")]
    public async Task<IActionResult> SubmitApprovalAction(
        Guid id,
        [FromBody] SpaApprovalActionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new SubmitApprovalActionCommand(id, request.ActionType, request.Comments),
            cancellationToken));

    [HttpPost("bulk-action")]
    public async Task<IActionResult> BulkSubmitApprovalActions(
        [FromBody] SpaBulkApprovalActionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.RequestIds is not { Count: > 0 })
        {
            return BadRequest(new { message = "Select at least one request." });
        }

        if (request.ActionType is not (ApprovalActionType.Approve or ApprovalActionType.Reject))
        {
            return BadRequest(new { message = "Bulk actions support Approve and Reject only." });
        }

        if (!User.IsInRole("Admin") && !User.IsInRole("Approver"))
        {
            return Forbid();
        }

        var result = await _mediator.Send(
            new BulkSubmitApprovalActionsCommand(request.RequestIds, request.ActionType, request.Comments),
            cancellationToken);

        return Ok(result);
    }
}
