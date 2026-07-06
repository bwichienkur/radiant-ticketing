using EnhancementHub.Application.Features.SystemIntelligence.Dtos;
using EnhancementHub.Application.Features.SystemIntelligence.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Refactor;

/// <summary>Legacy Razor refactor plans — redirects to <c>/Spa/Refactor/Plans</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/Refactor/Plans. Append ?layout=classic only for legacy Razor debugging.")]
[Authorize]
public class PlansModel : PageModel
{
    private readonly IMediator _mediator;
    public PlansModel(IMediator mediator) => _mediator = mediator;

    public IReadOnlyList<RefactorPlanDto> Plans { get; private set; } = [];
    public RefactorPlanDetailDto? SelectedPlan { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid? planId, string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return planId.HasValue
                ? RedirectPermanent($"/Spa/Refactor/Plans?planId={planId}")
                : RedirectToPagePermanent("/Spa/RefactorPlans");
        }

        Plans = await _mediator.Send(new ListRefactorPlansQuery(), cancellationToken);
        if (planId.HasValue)
        {
            SelectedPlan = await _mediator.Send(new GetRefactorPlanQuery(planId.Value), cancellationToken);
        }

        return Page();
    }
}
