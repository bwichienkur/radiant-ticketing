using EnhancementHub.Application.Features.Approvals.Commands;
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
    public async Task<IActionResult> ListPendingApprovals(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new ListEnhancementRequestsQuery(
                EnhancementRequestStatus.PendingApproval,
                Sort: EnhancementRequestSort.HighestRisk),
            cancellationToken));

    [HttpPost("{id:guid}/action")]
    public async Task<IActionResult> SubmitApprovalAction(
        Guid id,
        [FromBody] SpaApprovalActionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new SubmitApprovalActionCommand(id, request.ActionType, request.Comments),
            cancellationToken));
}
