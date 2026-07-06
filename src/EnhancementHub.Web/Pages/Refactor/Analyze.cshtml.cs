using EnhancementHub.Application.Features.Applications.Dtos;
using EnhancementHub.Application.Features.Applications.Queries;
using EnhancementHub.Application.Features.SystemIntelligence.Commands;
using EnhancementHub.Application.Features.SystemIntelligence.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Refactor;

/// <summary>Legacy Razor refactor analysis — redirects to <c>/Spa/Refactor/Analyze</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/Refactor/Analyze. Append ?layout=classic only for legacy Razor debugging.")]
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

    public async Task<IActionResult> OnGetAsync(string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToPagePermanent("/Spa/RefactorAnalyze");
        }

        Applications = await _mediator.Send(new ListApplicationsQuery(), cancellationToken);
        ApplicationId = Applications.FirstOrDefault()?.Id ?? Guid.Empty;
        return Page();
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
