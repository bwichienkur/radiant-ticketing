using EnhancementHub.Application.Features.Feedback.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Web.Controllers.Spa;

[ApiController]
[Authorize]
[Route("web-api/spa/feedback")]
public sealed class SpaFeedbackController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpaFeedbackController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Submit(
        [FromBody] SpaSubmitFeedbackRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new SubmitProductFeedbackCommand(request.WorkflowKey, request.NpsScore, request.Comment),
            cancellationToken));
}

public sealed record SpaSubmitFeedbackRequest(
    string WorkflowKey,
    int NpsScore,
    string? Comment);
