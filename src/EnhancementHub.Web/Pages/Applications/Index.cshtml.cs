using EnhancementHub.Application.Features.Applications.Dtos;
using EnhancementHub.Application.Features.Applications.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Applications;

/// <summary>Legacy Razor applications list — redirects to <c>/Spa/Applications</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/Applications. Append ?layout=classic only for legacy Razor debugging.")]
[Authorize]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IndexModel(IMediator mediator) => _mediator = mediator;

    public IReadOnlyList<ApplicationDto> Applications { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToPagePermanent("/Spa/Applications");
        }

        Applications = await _mediator.Send(new ListApplicationsQuery(), cancellationToken);
        return Page();
    }
}
