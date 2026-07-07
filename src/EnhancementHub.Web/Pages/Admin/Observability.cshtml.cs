using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Application.Features.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

/// <summary>Legacy Razor observability page — redirects to <c>/Spa/Admin/Observability</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/Admin/Observability. Append ?layout=classic only for legacy Razor debugging.")]
[Authorize(Roles = "Admin")]
public class ObservabilityModel : PageModel
{
    private readonly IMediator _mediator;

    public ObservabilityModel(IMediator mediator) => _mediator = mediator;

    public ObservabilityStatusDto? Status { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent("/Spa/Admin/Observability");
        }

        Status = await _mediator.Send(new GetObservabilityStatusQuery(), cancellationToken);
        return Page();
    }
}
