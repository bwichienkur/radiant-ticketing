using EnhancementHub.Application.Features.Reporting.Dtos;
using EnhancementHub.Application.Features.Reporting.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

/// <summary>Legacy Razor ROI dashboard — redirects to <c>/Spa/Insights</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/Insights. Append ?layout=classic only for legacy Razor debugging.")]
[Authorize(Roles = "Admin")]
public class RoiModel : PageModel
{
    private readonly IMediator _mediator;

    public RoiModel(IMediator mediator) => _mediator = mediator;

    public RoiReportDto? Report { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent("/Spa/Insights");
        }

        Report = await _mediator.Send(new GetRoiReportQuery(), cancellationToken);
        return Page();
    }
}
