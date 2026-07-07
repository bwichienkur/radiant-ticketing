using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Application.Admin;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Admin.Commands;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Application.Features.Admin.Queries;
using EnhancementHub.Application.Features.Billing.Commands;
using EnhancementHub.Application.Features.CustomFields.Commands;
using EnhancementHub.Application.Features.CustomFields.Queries;
using EnhancementHub.Application.Features.Tenants.Commands;
using EnhancementHub.Application.Features.Tenants.Queries;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Web.Controllers.Spa;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("web-api/spa/admin")]
public sealed class SpaAdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPlatformRuntimeStatusService _runtimeStatus;

    public SpaAdminController(IMediator mediator, IPlatformRuntimeStatusService runtimeStatus)
    {
        _mediator = mediator;
        _runtimeStatus = runtimeStatus;
    }

    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobsStatus(CancellationToken cancellationToken) =>
        Ok(new SpaAdminJobsResponse(
            await _mediator.Send(new GetBackgroundJobsStatusQuery(), cancellationToken),
            await _mediator.Send(new GetIndexFreshnessReportQuery(), cancellationToken)));

    [HttpPost("jobs/{jobId}/retry")]
    public async Task<IActionResult> RetryJob(string jobId, CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(new RetryBackgroundJobCommand(jobId), cancellationToken);
        return success
            ? Ok(new { message = "Failed job requeued successfully." })
            : NotFound(new { message = "Could not requeue job. Retry is only available for Hangfire failed jobs." });
    }

    [HttpGet("compliance/soc2")]
    public async Task<IActionResult> GetSoc2Readiness(CancellationToken cancellationToken) =>
        Ok(new SpaAdminComplianceResponse(
            await _mediator.Send(new GetSoc2ReadinessReportQuery(), cancellationToken),
            _runtimeStatus.GetStatus()));

    [HttpGet("custom-fields")]
    public async Task<IActionResult> ListCustomFields(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListCustomFieldDefinitionsQuery(ActiveOnly: false), cancellationToken));

    [HttpPost("custom-fields")]
    public async Task<IActionResult> UpsertCustomField(
        [FromBody] SpaUpsertCustomFieldRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var field = await _mediator.Send(
                new UpsertCustomFieldDefinitionCommand(
                    request.Id,
                    request.Key,
                    request.Label,
                    request.FieldType,
                    request.IsRequired,
                    request.IsActive,
                    request.SortOrder,
                    request.Options),
                cancellationToken);
            return Ok(field);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("custom-fields/{id:guid}")]
    public async Task<IActionResult> DeleteCustomField(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteCustomFieldDefinitionCommand(id), cancellationToken);
        return Ok(new { message = "Custom field deleted." });
    }

    [HttpGet("tenancy")]
    public async Task<IActionResult> GetTenancy(CancellationToken cancellationToken)
    {
        try
        {
            var billing = await _mediator.Send(new GetCurrentTenantBillingQuery(), cancellationToken);
            TenantIsolationStatus? isolation = null;
            try
            {
                isolation = await _mediator.Send(new GetCurrentTenantIsolationQuery(), cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                isolation = null;
            }

            return Ok(new SpaAdminTenancyResponse(billing, isolation, []));
        }
        catch (UnauthorizedAccessException)
        {
            var tenants = await _mediator.Send(new ListTenantsQuery(), cancellationToken);
            return Ok(new SpaAdminTenancyResponse(null, null, tenants));
        }
    }

    [HttpPost("tenancy/checkout")]
    public async Task<IActionResult> CreateCheckout(
        [FromBody] SpaBillingCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _mediator.Send(new CreateBillingCheckoutCommand(request.Plan), cancellationToken);
            return Ok(new { url = session.Url });
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpPost("tenancy/portal")]
    public async Task<IActionResult> CreatePortal(CancellationToken cancellationToken)
    {
        try
        {
            var session = await _mediator.Send(new CreateBillingPortalCommand(), cancellationToken);
            return Ok(new { url = session.Url });
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpPost("tenancy/provision-schema")]
    public async Task<IActionResult> ProvisionSchema(CancellationToken cancellationToken)
    {
        try
        {
            var isolation = await _mediator.Send(new ProvisionTenantSchemaCommand(), cancellationToken);
            return Ok(new { message = "Dedicated schema provisioned successfully.", isolation });
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpGet("observability")]
    public async Task<IActionResult> GetObservability(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetObservabilityStatusQuery(), cancellationToken));

    [HttpGet("data-scaling")]
    public async Task<IActionResult> GetDataScaling(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetDataScalingStatusQuery(), cancellationToken));

    [HttpGet("retention")]
    public async Task<IActionResult> GetRetention(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetDataRetentionStatusQuery(), cancellationToken));

    [HttpPost("retention/apply")]
    public async Task<IActionResult> ApplyRetention(
        [FromQuery] bool dryRun = false,
        CancellationToken cancellationToken = default) =>
        Ok(await _mediator.Send(new ApplyDataRetentionCommand(dryRun), cancellationToken));

    [HttpGet("ai-prompts")]
    public async Task<IActionResult> ListAiPrompts(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListAiPromptConfigurationsQuery(), cancellationToken));

    [HttpPut("ai-prompts/{id:guid}")]
    public async Task<IActionResult> UpdateAiPrompt(
        Guid id,
        [FromBody] SpaUpdateAiPromptRequest request,
        CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(
            new UpdateAiPromptConfigurationCommand(
                id,
                request.SystemPromptTemplate,
                request.UserPromptTemplate,
                request.IsActive),
            cancellationToken);
        return success ? Ok(new { message = "Prompt updated." }) : NotFound();
    }
}

public sealed record SpaAdminJobsResponse(
    BackgroundJobsStatusDto Status,
    IndexFreshnessReportDto Freshness);

public sealed record SpaAdminComplianceResponse(
    Soc2ReadinessReportDto Report,
    PlatformRuntimeStatus RuntimeStatus);

public sealed record SpaAdminTenancyResponse(
    Application.Features.Tenants.Dtos.TenantBillingDto? Billing,
    TenantIsolationStatus? Isolation,
    IReadOnlyList<Application.Features.Tenants.Dtos.TenantSummaryDto> AllTenants);

public sealed record SpaUpsertCustomFieldRequest(
    Guid? Id,
    string Key,
    string Label,
    CustomFieldType FieldType,
    bool IsRequired,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<string>? Options);

public sealed record SpaBillingCheckoutRequest(TenantPlan Plan);

public sealed record SpaUpdateAiPromptRequest(
    string SystemPromptTemplate,
    string UserPromptTemplate,
    bool IsActive);
