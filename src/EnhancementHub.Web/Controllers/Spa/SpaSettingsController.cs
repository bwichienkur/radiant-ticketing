using EnhancementHub.Application.Admin;
using EnhancementHub.Application.Common;
using EnhancementHub.Application.Features.Admin.Commands;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Application.Features.Admin.Queries;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Web.Controllers.Spa;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("web-api/spa/settings")]
public sealed class SpaSettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpaSettingsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("authentication")]
    public async Task<IActionResult> GetAuthenticationStatus(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetAuthenticationConfigurationStatusQuery(), cancellationToken));

    [HttpGet("system")]
    public async Task<IActionResult> ListSystemSettings(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetSystemSettingsQuery(), cancellationToken));

    [HttpPut("system/{settingId:guid}")]
    public async Task<IActionResult> UpdateSystemSetting(
        Guid settingId,
        [FromBody] SpaUpdateSystemSettingRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateSystemSettingCommand(settingId, request.Value), cancellationToken);
        return Ok(new { message = "Setting updated." });
    }

    [HttpGet("teams")]
    public async Task<IActionResult> ListTeams(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListTeamsQuery(), cancellationToken));

    [HttpPost("teams")]
    public async Task<IActionResult> CreateTeam(
        [FromBody] SpaCreateTeamRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _mediator.Send(
                new CreateTeamCommand(request.Name, request.Description),
                cancellationToken));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Errors.FirstOrDefault()?.ErrorMessage ?? ex.Message });
        }
    }

    [HttpGet("api-keys")]
    public async Task<IActionResult> ListApiKeys(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListServiceApiKeysQuery(), cancellationToken));

    [HttpPost("api-keys")]
    public async Task<IActionResult> CreateApiKey(
        [FromBody] SpaCreateApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _mediator.Send(
                new CreateServiceApiKeyCommand(
                    request.Name,
                    request.Description,
                    request.Role,
                    request.TeamId,
                    request.ExpiresInDays),
                cancellationToken));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Errors.FirstOrDefault()?.ErrorMessage ?? ex.Message });
        }
    }

    [HttpPost("api-keys/{keyId:guid}/revoke")]
    public async Task<IActionResult> RevokeApiKey(Guid keyId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RevokeServiceApiKeyCommand(keyId), cancellationToken);
        return Ok(new { message = "API key revoked." });
    }

    [HttpGet("webhooks")]
    public IActionResult GetWebhookMetadata() =>
        Ok(new SpaWebhookMetadataResponse(WebhookEventTypes.All));

    [HttpGet("webhooks/subscriptions")]
    public async Task<IActionResult> ListWebhookSubscriptions(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListWebhookSubscriptionsQuery(), cancellationToken));

    [HttpGet("webhooks/deliveries")]
    public async Task<IActionResult> ListWebhookDeliveries(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListWebhookDeliveriesQuery(50), cancellationToken));

    [HttpPost("webhooks/subscriptions")]
    public async Task<IActionResult> CreateWebhookSubscription(
        [FromBody] SpaCreateWebhookSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _mediator.Send(
                new CreateWebhookSubscriptionCommand(request.Name, request.Url, request.EventTypes),
                cancellationToken));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Errors.FirstOrDefault()?.ErrorMessage ?? ex.Message });
        }
    }

    [HttpPost("webhooks/subscriptions/{subscriptionId:guid}/revoke")]
    public async Task<IActionResult> RevokeWebhookSubscription(
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new RevokeWebhookSubscriptionCommand(subscriptionId), cancellationToken);
        return Ok(new { message = "Webhook subscription revoked." });
    }
}

public sealed record SpaUpdateSystemSettingRequest(string Value);

public sealed record SpaCreateTeamRequest(string Name, string? Description);

public sealed record SpaCreateApiKeyRequest(
    string Name,
    string? Description,
    UserRole Role,
    Guid? TeamId,
    int? ExpiresInDays);

public sealed record SpaCreateWebhookSubscriptionRequest(
    string Name,
    string Url,
    IReadOnlyList<string> EventTypes);

public sealed record SpaWebhookMetadataResponse(IReadOnlyList<string> EventTypes);
