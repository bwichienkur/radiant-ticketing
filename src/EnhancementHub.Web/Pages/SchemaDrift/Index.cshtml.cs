using EnhancementHub.Application.Features.SystemIntelligence.Commands;
using EnhancementHub.Application.Features.SystemIntelligence.Dtos;
using EnhancementHub.Application.Features.SystemIntelligence.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.SchemaDrift;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;
    public IndexModel(IMediator mediator) => _mediator = mediator;

    public IReadOnlyList<DatabaseConnectionDto> Connections { get; private set; } = [];
    public Guid? ConnectionId { get; private set; }
    public DriftReportDto? Report { get; private set; }

    public async Task OnGetAsync(Guid? connectionId, CancellationToken cancellationToken)
    {
        Connections = await _mediator.Send(new ListDatabaseConnectionsQuery(), cancellationToken);
        ConnectionId = connectionId ?? Connections.FirstOrDefault()?.Id;
        if (ConnectionId.HasValue)
        {
            Report = await _mediator.Send(new GetDriftReportQuery(ConnectionId.Value), cancellationToken);
        }
    }

    public async Task<IActionResult> OnGetDetectAsync(Guid connectionId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DetectSchemaDriftCommand(connectionId), cancellationToken);
        return RedirectToPage(new { connectionId });
    }
}
