using EnhancementHub.Application.Admin;
using EnhancementHub.Application.Features.Admin.Commands;
using EnhancementHub.Application.Features.Admin.Queries;
using EnhancementHub.Application.Features.Reporting.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator) => _mediator = mediator;

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings([FromQuery] string? category, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetSystemSettingsQuery(category), cancellationToken));

    [HttpPut("settings/{id:guid}")]
    public async Task<IActionResult> UpdateSetting(Guid id, [FromBody] UpdateSettingRequest request, CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(new UpdateSystemSettingCommand(id, request.Value), cancellationToken);
        return success ? NoContent() : NotFound();
    }

    [HttpGet("ai-prompts")]
    public async Task<IActionResult> GetAiPrompts(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListAiPromptConfigurationsQuery(), cancellationToken));

    [HttpGet("ai-usage")]
    public async Task<IActionResult> GetAiUsage(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetAiUsageReportQuery(), cancellationToken));

    [HttpGet("jobs/status")]
    public async Task<IActionResult> GetBackgroundJobsStatus(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetBackgroundJobsStatusQuery(), cancellationToken));

    [HttpPost("jobs/{jobId}/retry")]
    public async Task<IActionResult> RetryBackgroundJob(string jobId, CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(new RetryBackgroundJobCommand(jobId), cancellationToken);
        return success ? NoContent() : NotFound();
    }

    [HttpGet("authentication/status")]
    public async Task<IActionResult> GetAuthenticationStatus(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetAuthenticationConfigurationStatusQuery(), cancellationToken));

    [HttpPut("ai-prompts/{id:guid}")]
    public async Task<IActionResult> UpdateAiPrompt(Guid id, [FromBody] UpdatePromptRequest request, CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(new UpdateAiPromptConfigurationCommand(
            id,
            request.SystemPromptTemplate,
            request.UserPromptTemplate,
            request.IsActive), cancellationToken);
        return success ? NoContent() : NotFound();
    }

    public sealed record UpdateSettingRequest(string Value);
    public sealed record UpdatePromptRequest(string SystemPromptTemplate, string UserPromptTemplate, bool IsActive);
}
