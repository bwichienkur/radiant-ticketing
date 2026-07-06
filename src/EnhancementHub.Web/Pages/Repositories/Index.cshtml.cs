using EnhancementHub.Application.Features.Repositories.Commands;
using EnhancementHub.Application.Features.Repositories.Dtos;
using EnhancementHub.Application.Features.Repositories.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Repositories;

/// <summary>Legacy Razor repositories list — redirects to <c>/Spa/Repositories</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/Repositories. Append ?layout=classic only for legacy Razor debugging.")]
[Authorize]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IndexModel(IMediator mediator) => _mediator = mediator;

    public IReadOnlyList<RepositoryDto> Repositories { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToPagePermanent("/Spa/Repositories");
        }

        Repositories = await _mediator.Send(new ListRepositoriesQuery(), cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostTriggerAsync(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new TriggerRepositoryIndexingCommand(id), cancellationToken);
        return RedirectToPage();
    }
}
