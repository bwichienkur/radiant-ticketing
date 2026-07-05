using EnhancementHub.Application.Features.Analysis.Queries;
using EnhancementHub.Application.Features.Approvals.Commands;
using EnhancementHub.Application.Features.Approvals.Queries;
using EnhancementHub.Application.Features.EnhancementRequests.Queries;
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
}
