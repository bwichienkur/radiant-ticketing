using EnhancementHub.Application.Features.SystemIntelligence.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.DatabaseConnections;

/// <summary>Legacy Razor ERD page — redirects to <c>/Spa/DatabaseConnections/{id}/erd</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/DatabaseConnections/{id}/erd. Append ?layout=classic only for legacy Razor debugging.")]
[Authorize]
public class ErdModel : PageModel
{
    private readonly IMediator _mediator;
    public ErdModel(IMediator mediator) => _mediator = mediator;
    public string Mermaid { get; private set; } = "erDiagram\n  PLACEHOLDER { string empty }";

    public async Task<IActionResult> OnGetAsync(Guid id, string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent($"/Spa/DatabaseConnections/{id}/erd");
        }

        var connections = await _mediator.Send(new ListDatabaseConnectionsQuery(), cancellationToken);
        var connection = connections.First(c => c.Id == id);
        var erd = await _mediator.Send(new GetErdDiagramQuery(connection.ApplicationId), cancellationToken);
        Mermaid = string.IsNullOrWhiteSpace(erd.Mermaid) ? Mermaid : erd.Mermaid;
        return Page();
    }
}
