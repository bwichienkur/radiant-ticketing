using EnhancementHub.Application.Features.SystemIntelligence.Dtos;
using EnhancementHub.Application.Features.SystemIntelligence.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.DatabaseConnections;

/// <summary>Legacy Razor details page — redirects to <c>/Spa/DatabaseConnections/{id}</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/DatabaseConnections/{id}. Append ?layout=classic only for legacy Razor debugging.")]
[Authorize]
public class DetailsModel : PageModel
{
    private readonly IMediator _mediator;
    public DetailsModel(IMediator mediator) => _mediator = mediator;

    public Guid ConnectionId { get; private set; }
    public DatabaseSchemaDto? Schema { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent($"/Spa/DatabaseConnections/{id}");
        }

        ConnectionId = id;
        Schema = await _mediator.Send(new GetDatabaseSchemaQuery(id), cancellationToken);
        return Page();
    }
}
