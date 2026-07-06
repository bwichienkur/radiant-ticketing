using EnhancementHub.Application.Features.Approvals.Commands;
using EnhancementHub.Application.Features.Approvals.Queries;
using EnhancementHub.Application.Features.EnhancementRequests.Queries;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ApprovalsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ApprovalsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("pending")]
    public async Task<IActionResult> Pending(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ListEnhancementRequestsQuery(EnhancementRequestStatus.PendingApproval),
            cancellationToken);
        return Ok(result.Items);
    }

    [HttpGet("{requestId:guid}/history")]
    public async Task<IActionResult> History(Guid requestId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetApprovalHistoryQuery(requestId), cancellationToken));

    [HttpPost("{requestId:guid}/actions")]
    public async Task<IActionResult> Process(Guid requestId, [FromBody] ApprovalRequest request, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new SubmitApprovalActionCommand(requestId, request.ActionType, request.Comments, request.EnhancementAnalysisId), cancellationToken));

    [HttpPost("{requestId:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid requestId, [FromBody] CommentRequest request, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new AddCommentCommand(requestId, request.Content, request.IsInternal), cancellationToken));

    public sealed record ApprovalRequest(ApprovalActionType ActionType, string? Comments, Guid? EnhancementAnalysisId = null);
    public sealed record CommentRequest(string Content, bool IsInternal = false);
}
