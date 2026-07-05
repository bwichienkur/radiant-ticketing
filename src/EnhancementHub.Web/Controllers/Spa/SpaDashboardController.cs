using EnhancementHub.Application.Features.Onboarding.Dtos;
using EnhancementHub.Application.Features.Onboarding.Queries;
using EnhancementHub.Application.Features.Reporting.Dtos;
using EnhancementHub.Application.Features.Reporting.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Web.Controllers.Spa;

[ApiController]
[Authorize]
[Route("web-api/spa/dashboard")]
public sealed class SpaDashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpaDashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var report = await _mediator.Send(new GetDashboardReportQuery(), cancellationToken);
        var insights = await _mediator.Send(new GetDashboardInsightsQuery(), cancellationToken);
        var onboardingStatus = await _mediator.Send(new GetOnboardingStatusQuery(), cancellationToken);

        var isApprover = User.IsInRole("Admin") || User.IsInRole("Approver");
        var showOnboardingChecklist =
            !onboardingStatus.HasSystemGraph
            || onboardingStatus.ActiveSessionId.HasValue
            || onboardingStatus.ApplicationCount == 0;

        return Ok(new SpaDashboardResponse(report, insights, onboardingStatus, isApprover, showOnboardingChecklist));
    }
}

public sealed record SpaDashboardResponse(
    DashboardReportDto Report,
    DashboardInsightsDto Insights,
    OnboardingStatusDto OnboardingStatus,
    bool IsApprover,
    bool ShowOnboardingChecklist);
