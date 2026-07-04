using EnhancementHub.Application.Features.SystemIntelligence.Dtos;
using EnhancementHub.Application.Features.SystemIntelligence.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Refactor;

[Authorize]
public class PlansModel : PageModel
{
    private readonly IMediator _mediator;
    public PlansModel(IMediator mediator) => _mediator = mediator;

    public IReadOnlyList<RefactorPlanDto> Plans { get; private set; } = [];
    public RefactorPlanDetailDto? SelectedPlan { get; private set; }

    public async Task OnGetAsync(Guid? planId, CancellationToken cancellationToken)
    {
        Plans = await _mediator.Send(new ListRefactorPlansQuery(), cancellationToken);
        if (planId.HasValue)
        {
            SelectedPlan = await _mediator.Send(new GetRefactorPlanQuery(planId.Value), cancellationToken);
        }
    }
}
