using EnhancementHub.Application.Features.Admin.Queries;
using EnhancementHub.Application.Features.Admin.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

/// <summary>Legacy Razor authentication admin — redirects to <c>/Spa/Settings/Authentication</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/Settings/Authentication. Append ?layout=classic only for legacy Razor debugging.")]
[Authorize(Roles = "Admin")]
public class AuthenticationModel : PageModel
{
    private readonly IMediator _mediator;

    public AuthenticationModel(IMediator mediator) => _mediator = mediator;

    public AuthenticationConfigurationStatusDto? Status { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent("/Spa/Settings/Authentication");
        }

        Status = await _mediator.Send(new GetAuthenticationConfigurationStatusQuery(), cancellationToken);
        return Page();
    }
}
