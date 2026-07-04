using EnhancementHub.Application.Features.Onboarding.Dtos;
using EnhancementHub.Application.Features.Onboarding.Queries;
using EnhancementHub.Application.Features.Reporting.Dtos;
using EnhancementHub.Application.Features.Reporting.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IndexModel(IMediator mediator) => _mediator = mediator;

    public DashboardReportDto? Report { get; private set; }
    public OnboardingStatusDto? OnboardingStatus { get; private set; }

    public bool ShowOnboardingChecklist =>
        OnboardingStatus is not null
        && (!OnboardingStatus.HasSystemGraph
            || OnboardingStatus.ActiveSessionId.HasValue
            || OnboardingStatus.ApplicationCount == 0);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Report = await _mediator.Send(new GetDashboardReportQuery(), cancellationToken);
        OnboardingStatus = await _mediator.Send(new GetOnboardingStatusQuery(), cancellationToken);
    }
}
