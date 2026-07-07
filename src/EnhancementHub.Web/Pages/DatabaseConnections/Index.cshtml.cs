using EnhancementHub.Application.Features.SystemIntelligence.Commands;
using EnhancementHub.Application.Features.SystemIntelligence.Dtos;
using EnhancementHub.Application.Features.SystemIntelligence.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.DatabaseConnections;

/// <summary>Legacy Razor database connections list — redirects to <c>/Spa/DatabaseConnections</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/DatabaseConnections. Append ?layout=classic only for legacy Razor debugging.")]
[Authorize]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;
    public IndexModel(IMediator mediator) => _mediator = mediator;
    public IReadOnlyList<DatabaseConnectionDto> Connections { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToPagePermanent("/Spa/DatabaseConnections");
        }

        Connections = await _mediator.Send(new ListDatabaseConnectionsQuery(), cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostScanAsync(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new TriggerDatabaseScanCommand(id), cancellationToken);
        return RedirectToPage();
    }
}
