using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Application.Features.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

/// <summary>Legacy Razor data scaling page — redirects to <c>/Spa/Admin/DataScaling</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/Admin/DataScaling. Append ?layout=classic only for legacy Razor debugging.")]
[Authorize(Roles = "Admin")]
public class DataScalingModel : PageModel
{
    private readonly IMediator _mediator;

    public DataScalingModel(IMediator mediator) => _mediator = mediator;

    public DataScalingStatusDto? Status { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent("/Spa/Admin/DataScaling");
        }

        Status = await _mediator.Send(new GetDataScalingStatusQuery(), cancellationToken);
        return Page();
    }
}
