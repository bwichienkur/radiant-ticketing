using EnhancementHub.Application.Admin;
using EnhancementHub.Application.Features.Admin.Commands;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Application.Features.Admin.Queries;
using EnhancementHub.Application.Features.Reporting.Queries;
using EnhancementHub.Domain.Enums;
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

    [HttpGet("retention/status")]
    public async Task<IActionResult> GetDataRetentionStatus(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetDataRetentionStatusQuery(), cancellationToken));

    [HttpPost("retention/apply")]
    public async Task<IActionResult> ApplyDataRetention(
        [FromQuery] bool dryRun = false,
        CancellationToken cancellationToken = default) =>
        Ok(await _mediator.Send(new ApplyDataRetentionCommand(dryRun), cancellationToken));

    [HttpGet("compliance/soc2")]
    public async Task<IActionResult> GetSoc2ReadinessReport(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetSoc2ReadinessReportQuery(), cancellationToken));

    [HttpGet("indexing/freshness")]
    public async Task<IActionResult> GetIndexFreshnessReport(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetIndexFreshnessReportQuery(), cancellationToken));

    [HttpGet("teams")]
    public async Task<IActionResult> ListTeams(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListTeamsQuery(), cancellationToken));

    [HttpGet("teams/{teamId:guid}")]
    public async Task<IActionResult> GetTeam(Guid teamId, CancellationToken cancellationToken)
    {
        var team = await _mediator.Send(new GetTeamDetailQuery(teamId), cancellationToken);
        return team is null ? NotFound() : Ok(team);
    }

    [HttpPost("teams")]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamRequest request, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new CreateTeamCommand(request.Name, request.Description), cancellationToken));

    [HttpPost("teams/{teamId:guid}/members")]
    public async Task<IActionResult> AddTeamMember(
        Guid teamId,
        [FromBody] AddTeamMemberRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new AddTeamMemberCommand(
            teamId,
            request.Email,
            request.DisplayName,
            request.GlobalRole,
            request.TeamRole), cancellationToken));

    [HttpPut("teams/{teamId:guid}/members/{memberId:guid}")]
    public async Task<IActionResult> UpdateTeamMemberRole(
        Guid teamId,
        Guid memberId,
        [FromBody] UpdateTeamMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateTeamMemberRoleCommand(teamId, memberId, request.TeamRole), cancellationToken);
        return NoContent();
    }

    [HttpDelete("teams/{teamId:guid}/members/{memberId:guid}")]
    public async Task<IActionResult> RemoveTeamMember(
        Guid teamId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveTeamMemberCommand(teamId, memberId), cancellationToken);
        return NoContent();
    }

    [HttpGet("api-keys")]
    public async Task<IActionResult> ListServiceApiKeys(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListServiceApiKeysQuery(), cancellationToken));

    [HttpPost("api-keys")]
    public async Task<IActionResult> CreateServiceApiKey(
        [FromBody] CreateServiceApiKeyRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new CreateServiceApiKeyCommand(
            request.Name,
            request.Description,
            request.Role,
            request.TeamId,
            request.ExpiresInDays), cancellationToken));

    [HttpDelete("api-keys/{id:guid}")]
    public async Task<IActionResult> RevokeServiceApiKey(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RevokeServiceApiKeyCommand(id), cancellationToken);
        return NoContent();
    }

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
    public sealed record CreateTeamRequest(string Name, string? Description);
    public sealed record AddTeamMemberRequest(string Email, string DisplayName, UserRole GlobalRole, string TeamRole);
    public sealed record UpdateTeamMemberRoleRequest(string TeamRole);
    public sealed record CreateServiceApiKeyRequest(
        string Name,
        string? Description,
        UserRole Role,
        Guid? TeamId,
        int? ExpiresInDays);
}
