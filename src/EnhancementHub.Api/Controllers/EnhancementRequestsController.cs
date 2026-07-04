using EnhancementHub.Application.Features.EnhancementRequests.Commands;
using EnhancementHub.Application.Features.EnhancementRequests.Queries;
using EnhancementHub.Api.Extensions;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class EnhancementRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EnhancementRequestsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] EnhancementRequestStatus? status, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListEnhancementRequestsQuery(status), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetEnhancementRequestByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateEnhancementRequestCommand(
            request.Title,
            request.BusinessDescription,
            request.DesiredOutcome,
            request.Priority,
            request.TargetApplicationId,
            request.RequestedDueDate,
            request.Department,
            request.TeamId,
            request.SupportingNotes), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRequest request, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new UpdateEnhancementRequestCommand(
            id,
            request.Title,
            request.BusinessDescription,
            request.DesiredOutcome,
            request.Priority,
            request.TargetApplicationId,
            request.RequestedDueDate,
            request.Department,
            request.TeamId,
            request.SupportingNotes), cancellationToken));

    [HttpPost("{id:guid}/attachments")]
    [RequestSizeLimit(20_000_000)]
    [EnableRateLimiting(RateLimitingExtensions.UploadPolicy)]
    public async Task<IActionResult> UploadAttachment(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new UploadEnhancementAttachmentCommand(
            id,
            file.FileName,
            file.ContentType,
            stream), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}/download")]
    public async Task<IActionResult> DownloadAttachment(
        Guid id,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetEnhancementAttachmentDownloadQuery(id, attachmentId),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(result.PresignedDownloadUrl))
        {
            return Redirect(result.PresignedDownloadUrl);
        }

        return File(result.ContentStream!, result.ContentType, result.FileName);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CancelEnhancementRequestCommand(id), cancellationToken);
        return NoContent();
    }

    public sealed record CreateRequest(
        string Title,
        string BusinessDescription,
        string DesiredOutcome,
        string Priority,
        Guid? TargetApplicationId,
        DateTime? RequestedDueDate,
        string? Department,
        Guid? TeamId,
        string? SupportingNotes);

    public sealed record UpdateRequest(
        string Title,
        string BusinessDescription,
        string DesiredOutcome,
        string Priority,
        Guid? TargetApplicationId,
        DateTime? RequestedDueDate,
        string? Department,
        Guid? TeamId,
        string? SupportingNotes);
}
