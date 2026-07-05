using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Integrations.Commands;
using EnhancementHub.Application.Features.Integrations.Dtos;
using EnhancementHub.Application.Features.Integrations.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Route("api/integrations")]
public sealed class IntegrationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IChatIntakeService _chatIntake;
    private readonly IServiceNowSyncService _serviceNowSync;

    public IntegrationsController(
        IMediator mediator,
        IChatIntakeService chatIntake,
        IServiceNowSyncService serviceNowSync)
    {
        _mediator = mediator;
        _chatIntake = chatIntake;
        _serviceNowSync = serviceNowSync;
    }

    [Authorize]
    [HttpPost("openapi")]
    public async Task<IActionResult> RegisterOpenApi(
        [FromBody] RegisterOpenApiRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new RegisterOpenApiSpecCommand(request.ApplicationId, request.Name, request.SpecDocument),
            cancellationToken));

    [Authorize]
    [HttpGet("openapi")]
    public async Task<IActionResult> ListOpenApi(
        [FromQuery] Guid applicationId,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListOpenApiRegistrationsQuery(applicationId), cancellationToken));

    [Authorize]
    [HttpGet("openapi/{registrationId:guid}/endpoints")]
    public async Task<IActionResult> ListOpenApiEndpoints(
        Guid registrationId,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListOpenApiEndpointsQuery(registrationId), cancellationToken));

    [Authorize(Roles = "Admin,Developer")]
    [HttpPost("polyglot/symbols")]
    public async Task<IActionResult> IngestPolyglotSymbols(
        [FromBody] PolyglotSymbolsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new IngestPolyglotSymbolsCommand(request.RepositoryId, request.Language, request.Symbols),
            cancellationToken));

    [AllowAnonymous]
    [HttpPost("slack/intake")]
    public async Task<IActionResult> SlackIntake(
        [FromBody] SlackIntakeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _chatIntake.SubmitFromSlackAsync(
            new SlackIntakePayload(request.Text, request.UserName, request.ChannelName, request.ResponseUrl),
            cancellationToken);

        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [AllowAnonymous]
    [HttpPost("teams/intake")]
    public async Task<IActionResult> TeamsIntake(
        [FromHeader(Name = "X-EnhancementHub-Intake-Key")] string? intakeKey,
        [FromBody] TeamsIntakeRequest request,
        CancellationToken cancellationToken)
    {
        if (!ValidateTeamsIntakeKey(intakeKey))
        {
            return Unauthorized();
        }

        var result = await _chatIntake.SubmitFromTeamsAsync(
            new TeamsIntakePayload(request.Text, request.UserName, request.TargetApplicationId, request.TeamId),
            cancellationToken);

        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [AllowAnonymous]
    [HttpPost("servicenow/webhook")]
    public async Task<IActionResult> ServiceNowWebhook(
        [FromHeader(Name = "X-ServiceNow-Webhook-Secret")] string? secret,
        [FromBody] ServiceNowWebhookRequest request,
        CancellationToken cancellationToken)
    {
        if (!ValidateServiceNowSecret(secret))
        {
            return Unauthorized();
        }

        var result = await _serviceNowSync.ApplyInboundUpdateAsync(
            new ServiceNowInboundUpdate(request.ExternalId, request.State, request.ShortDescription),
            cancellationToken);

        return result.Succeeded ? Ok(result) : NotFound(result);
    }

    private bool ValidateTeamsIntakeKey(string? key)
    {
        var expected = HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["Integrations:Teams:IntakeSecret"];
        return string.IsNullOrWhiteSpace(expected)
            || string.Equals(expected, key, StringComparison.Ordinal);
    }

    private bool ValidateServiceNowSecret(string? secret)
    {
        var expected = HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["Integrations:ServiceNow:WebhookSecret"];
        return string.IsNullOrWhiteSpace(expected)
            || string.Equals(expected, secret, StringComparison.Ordinal);
    }

    public sealed record RegisterOpenApiRequest(Guid ApplicationId, string Name, string SpecDocument);

    public sealed record PolyglotSymbolsRequest(
        Guid RepositoryId,
        string Language,
        IReadOnlyList<PolyglotSymbolInput> Symbols);

    public sealed record SlackIntakeRequest(
        string Text,
        string? UserName,
        string? ChannelName,
        string? ResponseUrl);

    public sealed record TeamsIntakeRequest(
        string Text,
        string? UserName,
        Guid? TargetApplicationId,
        Guid? TeamId);

    public sealed record ServiceNowWebhookRequest(
        string ExternalId,
        string State,
        string? ShortDescription);
}
