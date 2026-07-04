using EnhancementHub.Application.Features.SystemIntelligence.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.DatabaseConnections;

[Authorize]
public class ErdModel : PageModel
{
    private readonly IMediator _mediator;
    public ErdModel(IMediator mediator) => _mediator = mediator;
    public string Mermaid { get; private set; } = "erDiagram\n  PLACEHOLDER { string empty }";

    public async Task OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var connections = await _mediator.Send(new ListDatabaseConnectionsQuery(), cancellationToken);
        var connection = connections.First(c => c.Id == id);
        var erd = await _mediator.Send(new GetErdDiagramQuery(connection.ApplicationId), cancellationToken);
        Mermaid = string.IsNullOrWhiteSpace(erd.Mermaid) ? Mermaid : erd.Mermaid;
    }
}
