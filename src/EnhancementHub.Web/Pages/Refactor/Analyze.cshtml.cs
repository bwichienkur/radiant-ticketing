using EnhancementHub.Application.Features.Applications.Dtos;
using EnhancementHub.Application.Features.Applications.Queries;
using EnhancementHub.Application.Features.SystemIntelligence.Commands;
using EnhancementHub.Application.Features.SystemIntelligence.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Refactor;

[Authorize]
public class AnalyzeModel : PageModel
{
    private readonly IMediator _mediator;
    public AnalyzeModel(IMediator mediator) => _mediator = mediator;

    [BindProperty]
    public Guid ApplicationId { get; set; }

    [BindProperty]
    public string Target { get; set; } = string.Empty;

    public IReadOnlyList<ApplicationDto> Applications { get; private set; } = [];
    public BlastRadiusResultDto? Result { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Applications = await _mediator.Send(new ListApplicationsQuery(), cancellationToken);
        ApplicationId = Applications.FirstOrDefault()?.Id ?? Guid.Empty;
    }

    public async Task OnPostAsync(CancellationToken cancellationToken)
    {
        Applications = await _mediator.Send(new ListApplicationsQuery(), cancellationToken);
        Result = await _mediator.Send(new AnalyzeRefactorBlastRadiusCommand(ApplicationId, Target), cancellationToken);
    }

    public async Task<IActionResult> OnPostGeneratePlanAsync(CancellationToken cancellationToken)
    {
        var plan = await _mediator.Send(new GenerateRefactorPlanCommand(ApplicationId, Target), cancellationToken);
        return RedirectToPage("Plans", new { planId = plan.Id });
    }
}
