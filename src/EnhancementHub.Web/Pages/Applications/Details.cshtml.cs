using EnhancementHub.Application.Features.Applications.Dtos;
using EnhancementHub.Application.Features.Applications.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Applications;

/// <summary>Legacy Razor application detail — redirects to <c>/Spa/Applications/{id}</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/Applications/{id}. Append ?layout=classic only for legacy Razor debugging.")]
[Authorize]
public class DetailsModel : PageModel
{
    private readonly IMediator _mediator;

    public DetailsModel(IMediator mediator) => _mediator = mediator;

    public ApplicationDto? Application { get; private set; }
    public IReadOnlyList<ApplicationProfileDto> Profiles { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id, string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent($"/Spa/Applications/{id}");
        }

        var apps = await _mediator.Send(new ListApplicationsQuery(), cancellationToken);
        Application = apps.FirstOrDefault(a => a.Id == id);
        if (Application is null) return NotFound();

        Profiles = await _mediator.Send(new GetApplicationProfileQuery(id), cancellationToken);
        return Page();
    }
}
