using EnhancementHub.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public sealed class WebhooksController : ControllerBase
{
    private readonly IGitHubWebhookService _gitHubWebhookService;
    private readonly IStripeBillingService _stripeBillingService;

    public WebhooksController(
        IGitHubWebhookService gitHubWebhookService,
        IStripeBillingService stripeBillingService)
    {
        _gitHubWebhookService = gitHubWebhookService;
        _stripeBillingService = stripeBillingService;
    }

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

    [AllowAnonymous]
    [HttpPost("stripe")]
    public async Task<IActionResult> Stripe(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();

        var result = await _stripeBillingService.HandleWebhookAsync(payload, signature, cancellationToken);
        return result.Accepted ? Ok(result) : Unauthorized(result);
    }
}
