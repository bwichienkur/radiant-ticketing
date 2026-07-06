using EnhancementHub.Application.Features.Delivery.Commands;
using EnhancementHub.Application.Features.Delivery.Queries;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Web.Controllers.Spa;

[ApiController]
[Authorize]
[Route("web-api/spa/delivery")]
public sealed class SpaDeliveryController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpaDeliveryController(IMediator mediator) => _mediator = mediator;

    [HttpGet("tenant-profile")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetTenantProfile(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetTenantDeliveryProfileQuery(), cancellationToken));

    [HttpPut("tenant-profile")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateTenantProfile(
        [FromBody] SpaTenantDeliveryProfileRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new UpdateTenantDeliveryProfileCommand(
                request.DefaultCicdProvider,
                request.VaultSecretPrefix,
                request.AutoImplementOnApprove,
                request.AutoDeployToTest,
                request.RequirePullRequestReview,
                request.RequireUatSignoff,
                request.RequireProdChangeWindow,
                request.ChangeWindowNotes,
                request.QaVideoRetentionDays),
            cancellationToken));

    [HttpPost("tenant-profile/validate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ValidateTenantProfile(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ValidateTenantDeliveryProfileQuery(), cancellationToken));

    [HttpPost("tenant-environments")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpsertTenantEnvironment(
        [FromBody] SpaTenantDeploymentEnvironmentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new UpsertTenantDeploymentEnvironmentCommand(
                request.EnvironmentId,
                request.Name,
                request.EnvironmentType,
                request.BaseUrlTemplate,
                request.SecretReferencePrefix,
                request.IsActive,
                request.SortOrder,
                request.RequiresApprovalForDeploy),
            cancellationToken));

    [HttpDelete("tenant-environments/{environmentId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTenantEnvironment(
        Guid environmentId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteTenantDeploymentEnvironmentCommand(environmentId), cancellationToken);
        return NoContent();
    }

    [HttpGet("applications/{applicationId:guid}/profile")]
    public async Task<IActionResult> GetApplicationProfile(
        Guid applicationId,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetApplicationDeliveryProfileQuery(applicationId), cancellationToken));

    [HttpPut("applications/{applicationId:guid}/profile")]
    public async Task<IActionResult> UpdateApplicationProfile(
        Guid applicationId,
        [FromBody] SpaApplicationDeliveryProfileRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new UpdateApplicationDeliveryProfileCommand(
                applicationId,
                request.DeploymentMechanism,
                request.PrimaryRepositoryId,
                request.BranchNamingPattern,
                request.CicdPipelineReference,
                request.CicdProviderOverride,
                request.SmokeTestPath,
                request.DatabaseMigrationStrategy,
                request.RequiresHumanProdDeploy,
                request.ConfigTransformsJson,
                request.ConnectionMappingsJson),
            cancellationToken));

    [HttpPost("applications/{applicationId:guid}/profile/validate")]
    public async Task<IActionResult> ValidateApplicationProfile(
        Guid applicationId,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ValidateApplicationDeliveryProfileQuery(applicationId), cancellationToken));
}

public sealed record SpaTenantDeliveryProfileRequest(
    CicdProvider DefaultCicdProvider,
    string? VaultSecretPrefix,
    bool AutoImplementOnApprove,
    bool AutoDeployToTest,
    bool RequirePullRequestReview,
    bool RequireUatSignoff,
    bool RequireProdChangeWindow,
    string? ChangeWindowNotes,
    int QaVideoRetentionDays);

public sealed record SpaTenantDeploymentEnvironmentRequest(
    Guid? EnvironmentId,
    string Name,
    DeploymentEnvironmentType EnvironmentType,
    string? BaseUrlTemplate,
    string? SecretReferencePrefix,
    bool IsActive,
    int SortOrder,
    bool RequiresApprovalForDeploy);

public sealed record SpaApplicationDeliveryProfileRequest(
    DeploymentMechanism DeploymentMechanism,
    Guid? PrimaryRepositoryId,
    string BranchNamingPattern,
    string? CicdPipelineReference,
    CicdProvider? CicdProviderOverride,
    string SmokeTestPath,
    DatabaseMigrationStrategy DatabaseMigrationStrategy,
    bool RequiresHumanProdDeploy,
    string? ConfigTransformsJson,
    string? ConnectionMappingsJson);
