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

    public sealed record SpaApprovalActionRequest(ApprovalActionType ActionType, string? Comments);

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
}
