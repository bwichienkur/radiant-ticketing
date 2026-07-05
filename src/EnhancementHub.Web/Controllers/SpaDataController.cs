using EnhancementHub.Application.Features.Analysis.Queries;
using EnhancementHub.Application.Features.Applications.Queries;
using EnhancementHub.Application.Features.Approvals.Commands;
using EnhancementHub.Application.Features.EnhancementRequests.Queries;
using EnhancementHub.Application.Features.Onboarding.Commands;
using EnhancementHub.Application.Features.Onboarding.Queries;
using EnhancementHub.Application.Features.Repositories.Commands;
using EnhancementHub.Application.Features.SystemIntelligence.Commands;
using EnhancementHub.Application.Features.SystemIntelligence.Queries;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace EnhancementHub.Web.Controllers;

[ApiController]
[Authorize]
[Route("web-api/spa")]
public sealed class SpaDataController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpaDataController(IMediator mediator) => _mediator = mediator;

    [HttpGet("requests/{id:guid}")]
    public async Task<IActionResult> GetRequest(Guid id, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetEnhancementRequestByIdQuery(id), cancellationToken));

    [HttpGet("analysis/{requestId:guid}")]
    public async Task<IActionResult> GetAnalysis(Guid requestId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _mediator.Send(new GetEnhancementAnalysisQuery(requestId), cancellationToken));
        }
        catch (Application.Common.Exceptions.NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("requests/{id:guid}/comments")]
    public async Task<IActionResult> AddComment(
        Guid id,
        [FromBody] SpaAddCommentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new AddCommentCommand(id, request.Content, request.IsInternal),
            cancellationToken));

    [HttpGet("applications")]
    public async Task<IActionResult> ListApplications(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListApplicationsQuery(), cancellationToken));

    [HttpGet("system-map/{applicationId:guid}")]
    public async Task<IActionResult> GetSystemMap(Guid applicationId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetSystemMapQuery(applicationId), cancellationToken));

    [HttpGet("approvals/pending")]
    public async Task<IActionResult> ListPendingApprovals(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new ListEnhancementRequestsQuery(
                EnhancementRequestStatus.PendingApproval,
                Sort: EnhancementRequestSort.HighestRisk),
            cancellationToken));

    [HttpPost("approvals/{id:guid}/action")]
    public async Task<IActionResult> SubmitApprovalAction(
        Guid id,
        [FromBody] SpaApprovalActionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new SubmitApprovalActionCommand(id, request.ActionType, request.Comments),
            cancellationToken));

    [HttpPost("onboarding/start")]
    public async Task<IActionResult> StartOnboarding(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new StartOnboardingSessionCommand(), cancellationToken));

    [HttpGet("onboarding/{sessionId:guid}")]
    public async Task<IActionResult> GetOnboardingSession(Guid sessionId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetOnboardingSessionQuery(sessionId), cancellationToken));

    [HttpGet("onboarding/{sessionId:guid}/review")]
    public async Task<IActionResult> GetOnboardingReview(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _mediator.Send(new GetOnboardingSessionQuery(sessionId), cancellationToken);
        if (!session.ApplicationId.HasValue)
        {
            return BadRequest(new { message = "Application has not been created yet." });
        }

        return Ok(await _mediator.Send(
            new GetOnboardingReviewQuery(session.ApplicationId.Value),
            cancellationToken));
    }

    [HttpPost("onboarding/validate-path")]
    public async Task<IActionResult> ValidateRepositoryPath(
        [FromBody] SpaValidatePathRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ValidateRepositoryPathQuery(request.Path), cancellationToken));

    [HttpPost("onboarding/{sessionId:guid}/basics")]
    public async Task<IActionResult> SubmitOnboardingBasics(
        Guid sessionId,
        [FromBody] SpaOnboardingBasicsRequest request,
        CancellationToken cancellationToken)
    {
        var application = await _mediator.Send(new CreateApplicationCommand(
            request.Name,
            request.BusinessDomain,
            request.Purpose,
            null,
            request.RiskSensitiveAreas,
            request.OwnerTeamName), cancellationToken);

        var session = await _mediator.Send(new AdvanceOnboardingSessionCommand(
            sessionId,
            OnboardingStep.ConnectCode,
            application.Id), cancellationToken);

        return Ok(session);
    }

    [HttpPost("onboarding/{sessionId:guid}/repository")]
    public async Task<IActionResult> SubmitOnboardingRepository(
        Guid sessionId,
        [FromBody] SpaOnboardingRepositoryRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _mediator.Send(new GetOnboardingSessionQuery(sessionId), cancellationToken);
        if (!session.ApplicationId.HasValue)
        {
            return BadRequest(new { message = "Complete application basics first." });
        }

        var validation = await _mediator.Send(new ValidateRepositoryPathQuery(request.RepositoryPath), cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { message = validation.ErrorMessage ?? "Repository path is not accessible." });
        }

        await _mediator.Send(new RegisterRepositoryCommand(
            session.ApplicationId.Value,
            request.RepositoryName.Trim(),
            request.RepositoryPath.Trim(),
            ExternalTicketProvider.GitHub,
            string.IsNullOrWhiteSpace(request.DefaultBranch) ? "main" : request.DefaultBranch.Trim()),
            cancellationToken);

        var advanced = await _mediator.Send(new AdvanceOnboardingSessionCommand(
            sessionId,
            OnboardingStep.ConnectDatabase,
            session.ApplicationId), cancellationToken);

        return Ok(advanced);
    }

    [HttpPost("onboarding/{sessionId:guid}/database")]
    public async Task<IActionResult> SubmitOnboardingDatabase(
        Guid sessionId,
        [FromBody] SpaOnboardingDatabaseRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _mediator.Send(new GetOnboardingSessionQuery(sessionId), cancellationToken);
        if (!session.ApplicationId.HasValue)
        {
            return BadRequest(new { message = "Complete application basics first." });
        }

        await _mediator.Send(new RegisterDatabaseConnectionCommand(
            session.ApplicationId.Value,
            request.ConnectionName.Trim(),
            request.Provider,
            request.ConnectionString.Trim(),
            request.IsReadOnly), cancellationToken);

        var advanced = await _mediator.Send(new AdvanceOnboardingSessionCommand(
            sessionId,
            OnboardingStep.RunDiscovery,
            session.ApplicationId,
            false), cancellationToken);

        return Ok(advanced);
    }

    [HttpPost("onboarding/{sessionId:guid}/skip-database")]
    public async Task<IActionResult> SkipOnboardingDatabase(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _mediator.Send(new GetOnboardingSessionQuery(sessionId), cancellationToken);
        var advanced = await _mediator.Send(new AdvanceOnboardingSessionCommand(
            sessionId,
            OnboardingStep.RunDiscovery,
            session.ApplicationId,
            true), cancellationToken);

        return Ok(advanced);
    }

    [HttpPost("onboarding/{sessionId:guid}/discovery")]
    public async Task<IActionResult> QueueOnboardingDiscovery(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _mediator.Send(new GetOnboardingSessionQuery(sessionId), cancellationToken);
        if (!session.ApplicationId.HasValue)
        {
            return BadRequest(new { message = "Application is required before discovery." });
        }

        var updated = await _mediator.Send(
            new QueueApplicationDiscoveryCommand(session.ApplicationId.Value, sessionId),
            cancellationToken);

        return Ok(updated);
    }

    [HttpPost("onboarding/{sessionId:guid}/complete")]
    public async Task<IActionResult> CompleteOnboarding(Guid sessionId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new CompleteOnboardingSessionCommand(sessionId), cancellationToken));

    [HttpPost("onboarding/{sessionId:guid}/advance-review")]
    public async Task<IActionResult> AdvanceOnboardingToReview(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _mediator.Send(new GetOnboardingSessionQuery(sessionId), cancellationToken);
        if (session.DiscoveryJobState != DiscoveryJobState.Completed)
        {
            return BadRequest(new { message = "Discovery must complete before review." });
        }

        return Ok(await _mediator.Send(new AdvanceOnboardingSessionCommand(
            sessionId,
            OnboardingStep.ReviewExport,
            session.ApplicationId), cancellationToken));
    }

    [HttpGet("onboarding/github-app/status")]
    public async Task<IActionResult> GetGitHubAppStatus(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetGitHubAppStatusQuery(), cancellationToken));

    [HttpPost("onboarding/{sessionId:guid}/upload-zip")]
    public async Task<IActionResult> UploadOnboardingZip(
        Guid sessionId,
        IFormFile zipFile,
        [FromForm] string? repositoryName,
        CancellationToken cancellationToken)
    {
        var session = await _mediator.Send(new GetOnboardingSessionQuery(sessionId), cancellationToken);
        if (!session.ApplicationId.HasValue)
        {
            return BadRequest(new { message = "Complete application basics first." });
        }

        if (zipFile is null || zipFile.Length == 0)
        {
            return BadRequest(new { message = "ZIP file is required." });
        }

        if (!string.Equals(Path.GetExtension(zipFile.FileName), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only .zip archives are supported." });
        }

        var name = string.IsNullOrWhiteSpace(repositoryName)
            ? Path.GetFileNameWithoutExtension(zipFile.FileName)
            : repositoryName.Trim();

        await using var stream = zipFile.OpenReadStream();
        var result = await _mediator.Send(new UploadRepositoryArchiveCommand(
            session.ApplicationId.Value,
            name,
            stream), cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.ErrorMessage ?? "Failed to extract repository archive." });
        }

        var advanced = await _mediator.Send(new AdvanceOnboardingSessionCommand(
            sessionId,
            OnboardingStep.ConnectDatabase,
            session.ApplicationId), cancellationToken);

        return Ok(advanced);
    }

    [HttpPost("onboarding/{sessionId:guid}/clone-github-app")]
    public async Task<IActionResult> CloneGitHubAppRepository(
        Guid sessionId,
        [FromBody] SpaGitHubAppCloneRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _mediator.Send(new GetOnboardingSessionQuery(sessionId), cancellationToken);
        if (!session.ApplicationId.HasValue)
        {
            return BadRequest(new { message = "Complete application basics first." });
        }

        var result = await _mediator.Send(new CloneGitHubAppRepositoryCommand(
            session.ApplicationId.Value,
            request.RepositoryName,
            request.Owner,
            request.Repository,
            request.DefaultBranch,
            request.InstallationId), cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.ErrorMessage ?? "GitHub App clone failed." });
        }

        var advanced = await _mediator.Send(new AdvanceOnboardingSessionCommand(
            sessionId,
            OnboardingStep.ConnectDatabase,
            session.ApplicationId), cancellationToken);

        return Ok(advanced);
    }

    [HttpPost("onboarding/{sessionId:guid}/clone-git")]
    public async Task<IActionResult> CloneGitRepository(
        Guid sessionId,
        [FromBody] SpaGitCloneRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _mediator.Send(new GetOnboardingSessionQuery(sessionId), cancellationToken);
        if (!session.ApplicationId.HasValue)
        {
            return BadRequest(new { message = "Complete application basics first." });
        }

        var result = await _mediator.Send(new CloneGitRepositoryCommand(
            session.ApplicationId.Value,
            request.RepositoryName,
            request.RepositoryUrl,
            request.DefaultBranch,
            request.AccessToken), cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.ErrorMessage ?? "Git clone failed." });
        }

        var advanced = await _mediator.Send(new AdvanceOnboardingSessionCommand(
            sessionId,
            OnboardingStep.ConnectDatabase,
            session.ApplicationId), cancellationToken);

        return Ok(advanced);
    }

    [HttpPost("onboarding/build-connection-string")]
    public async Task<IActionResult> BuildConnectionString(
        [FromBody] SpaBuildConnectionStringRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new BuildDatabaseConnectionStringQuery(
            request.Provider,
            request.Host,
            request.Port,
            request.Database,
            request.Username,
            request.Password,
            request.IntegratedSecurity), cancellationToken));

    [HttpPost("onboarding/{sessionId:guid}/on-prem-agent")]
    public async Task<IActionResult> SetupOnPremAgent(
        Guid sessionId,
        [FromBody] SpaOnPremAgentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new SetupOnPremAgentForOnboardingCommand(
            sessionId,
            request.ApplicationId,
            request.ConnectionName,
            request.Provider), cancellationToken));

    [HttpGet("onboarding/{sessionId:guid}/export-docs")]
    public async Task<IActionResult> ExportOnboardingDocs(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _mediator.Send(new GetOnboardingSessionQuery(sessionId), cancellationToken);
        if (!session.ApplicationId.HasValue)
        {
            return BadRequest(new { message = "Application is required before export." });
        }

        var export = await _mediator.Send(
            new ExportDocumentationCommand(session.ApplicationId.Value, DocumentationExportFormat.Both),
            cancellationToken);

        return File(Encoding.UTF8.GetBytes(export.Content), export.ContentType, export.FileName);
    }

    public sealed record SpaApprovalActionRequest(ApprovalActionType ActionType, string? Comments);

    public sealed record SpaAddCommentRequest(string Content, bool IsInternal = false);

    public sealed record SpaValidatePathRequest(string Path);

    public sealed record SpaOnboardingBasicsRequest(
        string Name,
        string? BusinessDomain,
        string? Purpose,
        string? RiskSensitiveAreas,
        string? OwnerTeamName);

    public sealed record SpaOnboardingRepositoryRequest(
        string RepositoryName,
        string RepositoryPath,
        string DefaultBranch);

    public sealed record SpaOnboardingDatabaseRequest(
        string ConnectionName,
        DatabaseProviderType Provider,
        string ConnectionString,
        bool IsReadOnly);

    public sealed record SpaGitHubAppCloneRequest(
        string RepositoryName,
        string Owner,
        string Repository,
        string DefaultBranch,
        long? InstallationId);

    public sealed record SpaGitCloneRequest(
        string RepositoryName,
        string RepositoryUrl,
        string DefaultBranch,
        string? AccessToken);

    public sealed record SpaBuildConnectionStringRequest(
        DatabaseProviderType Provider,
        string Host,
        int Port,
        string Database,
        string? Username,
        string? Password,
        bool IntegratedSecurity);

    public sealed record SpaOnPremAgentRequest(
        Guid ApplicationId,
        string ConnectionName,
        DatabaseProviderType Provider);
}
