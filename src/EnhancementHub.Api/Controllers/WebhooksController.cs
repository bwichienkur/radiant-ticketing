using EnhancementHub.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public sealed class WebhooksController : ControllerBase
{
    private readonly IGitHubWebhookService _gitHubWebhookService;

    public WebhooksController(IGitHubWebhookService gitHubWebhookService) =>
        _gitHubWebhookService = gitHubWebhookService;

    [AllowAnonymous]
    [HttpPost("github")]
    public async Task<IActionResult> GitHub(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
        var eventName = Request.Headers["X-GitHub-Event"].FirstOrDefault();

        if (!string.Equals(eventName, "push", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new { message = "Event ignored." });
        }

        var result = await _gitHubWebhookService.HandlePushAsync(payload, signature, cancellationToken);
        return result.Accepted ? Ok(result) : Unauthorized(result);
    }
}
