using EnhancementHub.Application.Features.EnhancementRequests.Dtos;
using EnhancementHub.Application.Features.EnhancementRequests.Queries;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.EnhancementRequests;

/// <summary>Legacy Razor request list — redirects to <c>/Spa/RequestList</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/RequestList. Append ?layout=classic only for legacy Razor debugging.")]
[Authorize]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IndexModel(IMediator mediator) => _mediator = mediator;

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public EnhancementRequestStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Priority { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? View { get; set; }

    [BindProperty(SupportsGet = true)]
    public EnhancementRequestSort Sort { get; set; } = EnhancementRequestSort.Newest;

    public IReadOnlyList<EnhancementRequestDto> Requests { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToPagePermanent("/Spa/RequestList", new { q = Q, status = Status, priority = Priority, view = View, sort = Sort });
        }

        RiskLevel? minRisk = View == "highrisk" ? RiskLevel.High : null;
        if (View == "mine" && User.Identity?.Name is not null)
        {
            Q = User.Identity.Name.Contains('@')
                ? User.Identity.Name.Split('@')[0]
                : User.Identity.Name;
        }

        Requests = (await _mediator.Send(
            new ListEnhancementRequestsQuery(
                Status,
                Search: Q,
                Priority: Priority,
                MinRisk: minRisk,
                Sort: Sort),
            cancellationToken)).Items;

        return Page();
    }
}
